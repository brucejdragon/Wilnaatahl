namespace Wilnaatahl.ViewModel

open Wilnaatahl.Model
open Wilnaatahl.Model.FamilyGraph
open Wilnaatahl.Model.Initial
open Wilnaatahl.ViewModel.NodeState
open Wilnaatahl.ViewModel.UndoableState
#if FABLE_COMPILER
open Fable.Core
#endif

type DragData =
    {
        /// The position of the node that started the drag;
        /// Used to calculate new positions during the drag.
        Origin: float * float * float

        /// Last node to be touched before the drag operation started.
        LastTouchedNodeId: NodeId
    }

type DragState =
    | Dragging of DragData
    // Captures the state between pointer up and the final click, which should be ignored.
    | DragEnding
    | NotDragging

    member this.ShouldEnableOrbitControls =
        match this with
        | NotDragging -> true
        | DragEnding
        | Dragging _ -> false

#if FABLE_COMPILER
[<StringEnum>]
#endif
type SelectionMode =
    | SingleSelect
    | MultiSelect

    member this.IsSingleSelectEnabled =
        match this with
        | SingleSelect -> true
        | MultiSelect -> false

type Msg =
    | SelectNode of NodeId
    | DeselectAll
    | StartDrag
    | DragBy of float * float * float
    | EndDrag
    | ToggleSelection of SelectionMode
    | TouchNode of NodeId // In this context, "touch" means "pointer down".
    | Undo
    | Redo
    | Animate of NodeId * float * float * float

/// This is a handy data structure for rendering the connectors between members of an
/// immediate family.
type RenderedFamily = { Parents: NodeId * NodeId; Children: NodeId list }

module ViewState =
    type ViewState =
        private
            { History: UndoableState<NodeState>
              Families: RenderedFamily list
              Drag: DragState
              LastTouchedNodeId: NodeId option
              SelectionMode: SelectionMode }

    let createViewState nodes families =
        { History = createNodeState nodes |> createUndoableState
          Families = List.ofSeq families
          Drag = NotDragging
          LastTouchedNodeId = None
          SelectionMode = SingleSelect }

    let private areAnyNodesAnimating state =
        state.History |> current |> all |> Seq.exists (fun n -> n.IsAnimating)

    let canRedo state =
        canRedo state.History && not (areAnyNodesAnimating state)

    let canUndo state =
        canUndo state.History && not (areAnyNodesAnimating state)

    let enumerateFamilies state = state.Families

    let enumerateChildren state family =
        let nodes = current state.History

        family.Children
        |> List.map (fun childId -> nodes |> findNode childId)
        |> List.toSeq

    let enumerateParents state family =
        let nodes = current state.History
        let parent1Id, parent2Id = family.Parents
        let parent1 = nodes |> findNode parent1Id
        let parent2 = nodes |> findNode parent2Id
        parent1, parent2

    let enumerateSelectedTreeNodes state = state.History |> current |> selected
    let enumerateUnselectedTreeNodes state = state.History |> current |> unselected

    let isSingleSelectEnabled state =
        state.SelectionMode.IsSingleSelectEnabled

    let shouldEnableOrbitControls state = state.Drag.ShouldEnableOrbitControls

    let update state msg =
        let nodes = current state.History
        let commit nodeState = state.History |> setCurrent nodeState

        match msg with
        | Animate(nodeId, x, y, z) ->
            let newPosition = x, y, z
            let node = nodes |> findNode nodeId
            let delta = 0.01

            let isAnimationFinished =
                let tx, ty, tz = node.TargetPosition
                abs (x - tx) < delta && abs (y - ty) < delta && abs (z - tz) < delta

            let newNode =
                { node with
                    Position =
                        if isAnimationFinished then
                            node.TargetPosition
                        else
                            newPosition
                    IsAnimating = not isAnimationFinished }

            let updatedNodes = nodes |> replace nodeId newNode

            { state with History = commit updatedNodes }
        | SelectNode nodeId ->
            if nodes |> isSelected nodeId then
                match state.Drag with
                | NotDragging ->
                    // De-select currently selected node.
                    { state with History = nodes |> deselect nodeId |> commit }
                | DragEnding ->
                    // Ignore the click that ended the drag, as it was not a selection change.
                    { state with Drag = NotDragging }
                | Dragging _ -> state // Shouldn't happen, so ignore it.
            else
                // Select new node:
                // - In SingleSelect mode, this either selects a node for the first time or replaces the previous selection.
                // - In MultiSelect mode, this adds to the current selection.
                match state.SelectionMode with
                | SingleSelect ->
                    { state with
                        History = nodes |> deselectAll |> select nodeId |> commit
                        Drag = NotDragging }
                | MultiSelect ->
                    { state with
                        History = nodes |> select nodeId |> commit
                        Drag = NotDragging }
        | DeselectAll ->
            { state with
                History = nodes |> deselectAll |> commit
                Drag = NotDragging }
        | StartDrag ->
            match state.LastTouchedNodeId with
            | Some nodeId ->
                let node = nodes |> findNode nodeId

                // Dragging is not allowed on animating nodes since it does
                // weird things like snapshot nodes in the middle of automatic layout.
                if node.IsAnimating then
                    state
                else
                    // Use this opportunity to save the current node positions before
                    // they start changing for undo/redo.
                    { state with
                        History = state.History |> saveCurrentForUndo
                        Drag = Dragging { Origin = node.Position; LastTouchedNodeId = nodeId } }
            | None -> state // Shouldn't happen; Do nothing.
        | DragBy(moveX, moveY, moveZ) ->
            match state.Drag with
            | Dragging { Origin = originX, originY, originZ; LastTouchedNodeId = nodeId } ->

                // Find the previous position of the dragged node
                let node = nodes |> findNode nodeId
                let oldX, oldY, oldZ = node.Position
                let newX, newY, newZ = originX + moveX, originY + moveY, originZ + moveZ
                let dx, dy, dz = newX - oldX, newY - oldY, newZ - oldZ

                let updateNodePosition node =
                    let nx, ny, nz = node.Position
                    let newPos = nx + dx, ny + dy, nz + dz
                    { node with Position = newPos }

                let updatedNodes = nodes |> mapSelected updateNodePosition

                { state with History = commit updatedNodes }
            | DragEnding
            | NotDragging -> state
        | EndDrag ->
            match state.Drag with
            | Dragging _ ->
                // Drag is ending; Flush the redo history to avoid massive time-travel
                // confusion for the user.
                { state with History = clearRedo state.History; Drag = DragEnding }
            | DragEnding
            | NotDragging -> state // This can happen on de-selection clicks, so ignore it.
        | ToggleSelection mode ->
            // We clear the selection when toggling selection mode so you don't end up
            // confusing the user by having multiple nodes selected when in single-selection mode.
            { state with
                History = nodes |> deselectAll |> commit
                SelectionMode = mode }
        | TouchNode nodeId -> { state with LastTouchedNodeId = Some nodeId }
        | Undo ->
            let undoneHistory = undo state.History

            let newNodes =
                current state.History |> animateToNewNodePositions (current undoneHistory)

            { state with History = undoneHistory |> setCurrent newNodes }
        | Redo ->
            let redoneHistory = redo state.History

            let newNodes =
                current state.History |> animateToNewNodePositions (current redoneHistory)

            { state with History = redoneHistory |> setCurrent newNodes }

open ViewState

// Wrap ViewState functionality in an interface for easier consumption from TypeScript.
type IViewModel =
    abstract CanRedo: ViewState -> bool
    abstract CanUndo: ViewState -> bool
    abstract CreateInitialViewState: (seq<TreeNode> * seq<RenderedFamily>) -> ViewState
    abstract EnumerateFamilies: ViewState -> seq<RenderedFamily>
    abstract EnumerateChildren: ViewState -> RenderedFamily -> seq<TreeNode>
    abstract EnumerateParents: ViewState -> RenderedFamily -> TreeNode * TreeNode
    abstract EnumerateSelectedTreeNodes: ViewState -> seq<TreeNode>
    abstract EnumerateUnselectedTreeNodes: ViewState -> seq<TreeNode>
    abstract IsSingleSelectEnabled: ViewState -> bool
    abstract ShouldEnableOrbitControls: ViewState -> bool
    abstract Update: ViewState -> Msg -> ViewState

type ViewModel() =
    interface IViewModel with
        member _.CanRedo state = canRedo state
        member _.CanUndo state = canUndo state

        // This is intentionally a single argument of tuple type so that useReducer can pass in a single value.
        member _.CreateInitialViewState((nodes, families)) = createViewState nodes families
        member _.EnumerateFamilies state = enumerateFamilies state
        member _.EnumerateChildren state family = enumerateChildren state family
        member _.EnumerateParents state family = enumerateParents state family
        member _.EnumerateSelectedTreeNodes state = enumerateSelectedTreeNodes state
        member _.EnumerateUnselectedTreeNodes state = enumerateUnselectedTreeNodes state
        member _.IsSingleSelectEnabled state = isSingleSelectEnabled state
        member _.ShouldEnableOrbitControls state = shouldEnableOrbitControls state
        member _.Update state msg = update state msg

module Scene =
    /// Unit of measure (wd means "world delta") that represents relative co-ordinates in world space.
    [<Measure>]
    type wd

    /// Unit of measure (ud means "upper box delta") that represents relative co-ordinates in the frame of reference of
    /// the upper box during a vertical attach operation.
    [<Measure>]
    type ud

    /// Unit of measure (ld means "lower box delta") that represents relative co-ordinates in the frame of reference of
    /// the lower box during a vertical attach operation.
    [<Measure>]
    type ld

    // Define some conversion constants for our units.
    let private ld2ud = 1.0<ud / ld>
    let private wd2ld = 1.0<ld / wd>
    let private wd2ud = 1.0<ud / wd>
    let private ud2ld = 1.0<ld / ud>

    /// Represents a delta vector in 3D grid-space. The unit is specific to a frame of reference to
    /// help prevent mixing up co-ordinate spaces in layout calculations.
    type Vector<[<Measure>] 'u> =
        { X: float<'u>
          Y: float<'u>
          Z: float<'u> }

        static member inline (+)(lhs, rhs) =
            { X = lhs.X + rhs.X; Y = lhs.Y + rhs.Y; Z = lhs.Z + rhs.Z }

    /// Defines a frame of reference to help set the positions of points and other LayoutBoxes in world space.
    type private LayoutBox<[<Measure>] 'u> =
        private
            { Size: Vector<'u>
              ConnectX: float<'u>
              Payload: LayoutBoxPayload<'u> }

    /// Contains the parts of a LayoutBox that vary based on whether it contains other LayoutBoxes or not.
    and private LayoutBoxPayload<[<Measure>] 'u> =
        | Leaf of PersonId * Vector<'u>
        | Composite of CompositeLayoutBox<'u>

    /// Contains the parts of a LayoutBox that vary based on whether it contains other LayoutBoxes or not.
    and private CompositeLayoutBox<[<Measure>] 'u> = { Followers: (LayoutBox<'u> * Vector<'u>)[] }

    /// Translate a vector magnitude component from upper delta to lower delta space by adding a lower delta offset.
    /// Geometrically, this means "logically" moving the magnitude along its axis so its origin lines up with the
    /// lower delta co-ordinate space, and then adding the offset to grow or shrink the magnitude as desired (depending
    /// on its sign). The "logical" movement to align origins is entirely conceptual and implemented at compile-time
    /// thanks to the miracle of F# Units of Measure.
    let private translateToLower (magnitude: float<ud>) (offset: float<ld>) : float<ld> = magnitude * ud2ld + offset

    /// Like translateUpper, but does the reverse conversion (lower delta to upper delta relative to an upper space offset).
    let private translateToUpper (magnitude: float<ld>) (offset: float<ud>) : float<ud> = magnitude * ld2ud + offset

    let reframeVector<[<Measure>] 'u, [<Measure>] 'v>
        (conversionFactor: float<'v / 'u>)
        (vec: Vector<'u>)
        : Vector<'v> =
        { X = vec.X * conversionFactor
          Y = vec.Y * conversionFactor
          Z = vec.Z * conversionFactor }

    module private LayoutBox =
        let createLeaf size connectX personId offset =
            { Size = size; ConnectX = connectX; Payload = Leaf(personId, offset) }

        let createComposite size connectX followersWithOffsets =
            { Size = size
              ConnectX = connectX
              Payload = Composite { Followers = followersWithOffsets } }

        let rec reframeBox<[<Measure>] 'u, [<Measure>] 'v>
            (conversionFactor: float<'v / 'u>)
            (box: LayoutBox<'u>)
            : LayoutBox<'v> =
            let reframePayload payload =
                let reframeFollower (follower, offset) =
                    follower |> reframeBox conversionFactor, offset |> reframeVector conversionFactor

                match payload with
                | Leaf(personId, offset) -> Leaf(personId, offset |> reframeVector conversionFactor)
                | Composite composite -> Composite { Followers = composite.Followers |> Array.map reframeFollower }

            { Size = box.Size |> reframeVector conversionFactor
              ConnectX = box.ConnectX * conversionFactor
              Payload = box.Payload |> reframePayload }

        let rec setPosition pos box =
            seq {
                match box.Payload with
                | Leaf(personId, offset) -> yield personId, pos + offset
                | Composite composite ->
                    yield!
                        composite.Followers
                        |> Array.map (fun (followerBox, offset) -> followerBox |> setPosition (pos + offset))
                        |> Seq.concat
            }

        /// Creates a new box followed by all the given boxes that lays them out horizontally,
        /// in the order given from left to right (lower to highers X co-ordinate), aligned to
        /// the highest Y co-ordinate of the tallest box and sized to the deepest Z size.
        let attachHorizontally<[<Measure>] 'u> (boxes: LayoutBox<'u>[]) =
            let combineConnectXs (boxes: LayoutBox<'u>[]) =
                let boxCount = boxes.Length
                let middle = (boxCount - 1) / 2
                let distanceToLeft = boxes[0 .. middle - 1] |> Array.sumBy _.Size.X

                if boxCount % 2 <> 0 then
                    distanceToLeft + boxes[middle].ConnectX
                else
                    let left, right = boxes[middle], boxes[middle + 1]

                    // Trust me, it works, I did the math.
                    (2.0 * distanceToLeft + left.ConnectX + left.Size.X + right.ConnectX) / 2.0

            // Base case is that there is just one box, in which case we return it.
            if boxes.Length = 1 then
                boxes[0]
            else
                let size =
                    { X = boxes |> Seq.sumBy _.Size.X
                      Y = boxes |> Seq.map _.Size.Y |> Seq.max
                      Z = boxes |> Seq.map _.Size.Z |> Seq.max }

                let connectX = combineConnectXs boxes

                let followNext distanceToTheLeft box =
                    (box, { X = distanceToTheLeft; Y = size.Y - box.Size.Y; Z = 0.0<_> }),
                    distanceToTheLeft + box.Size.X

                let followers, _ = boxes |> Array.mapFold followNext 0.0<_>

                createComposite size connectX followers

        /// Attaches two boxes vertically, taking into account possible skew on the X axis.
        /// The given offset is the position of the upper box relative to the lower on the X axis.
        /// If the value is 0, the two are aligned on the left edge. If it's negative, the upper box
        /// extends to the left of the lower one, and its left edge will be the zero X point of the new
        /// box. If it's positive, the lower box extends to the left of the upper one, and its left edge
        /// will be the zero X point of the new box. After attaching the boxes, the ConnectX of the new
        /// box is based on that of the lower box.
        let attachVertically
            useUpperConnectX
            (upperOffset: float<ld>)
            (lowerBox: LayoutBox<ld>)
            (upperBox: LayoutBox<ud>)
            : LayoutBox<ld> =

            // The offset is in lower co-ordinate space, so we have to translate first.
            let upperOffsetX = max (translateToUpper upperOffset 0.0<ud>) 0.0<ud>

            // The offset is already in lower co-ordinate space, so the calculation is straightforward.
            let lowerOffsetX = max -upperOffset 0.0<ld>

            // Take into account skew, so if one box doesn't completely encompass the other's width,
            // we get the correct overall width. Since the offset is relative to the lower box, it should
            // add to the upper box when positive, and add to the lower box when negative.
            let adjustedLowerBoxWidth = lowerOffsetX + lowerBox.Size.X
            let adjustedHigherBoxWidth = upperOffsetX + upperBox.Size.X

            let size =
                { X = max adjustedLowerBoxWidth (adjustedHigherBoxWidth * ud2ld)
                  Y = translateToLower upperBox.Size.Y lowerBox.Size.Y
                  Z = max (upperBox.Size.Z * ud2ld) lowerBox.Size.Z }

            // We also need to translate the X connector in case the offset is non-negative, because that means the
            // upper box is not vertically aligned to the left edge of the new world-space box (which is aligned to
            // the leftmost of the upper and lower boxes).
            let connectX =
                if useUpperConnectX then
                    translateToLower (upperOffsetX + upperBox.ConnectX) 0.0<ld>
                else
                    lowerOffsetX + lowerBox.ConnectX

            let lowerFollowerOffset = { X = lowerOffsetX; Y = 0.0<ld>; Z = 0.0<ld> }

            let upperFollowerOffset =
                { X = upperOffsetX; Y = lowerBox.Size.Y * ld2ud; Z = 0.0<ud> }

            createComposite
                size
                connectX
                [| lowerBox, lowerFollowerOffset
                   upperBox |> reframeBox ud2ld, upperFollowerOffset |> reframeVector ud2ld |]

    let private leafWidth = 1.95<wd>
    let private familyHeight = 2.0<ud>
    let private coparentWidth = 1.95<wd>
    let private origin = { X = 0.0<ld>; Y = 0.0<ld>; Z = 0.0<ld> }

    // Used to sort people for layout by comparing Date of Birth (DoB), or birth order if DoB is missing.
    let private comparePeople person1 person2 =
        match person1.DateOfBirth, person2.DateOfBirth with
        | Some dob1, Some dob2 ->
            // Fable doesn't appear to support DateOnly.CompareTo.
            if dob1 < dob2 then -1
            elif dob1 > dob2 then 1
            else 0

        // If either person is missing a birth date, fall back on birth order.
        | Some _, None
        | None, Some _
        | None, None -> person1.BirthOrder - person2.BirthOrder

    let private leafBox<[<Measure>] 'u> (conversionFactor: float<'u / wd>) height personId =
        let connectX = leafWidth / 2.0 * conversionFactor

        // When height = 0, this is effectively a 1-dimensional line in 3D space, but that's ok and
        // it makes the layout math easier. With any height, the Person shape centerpoint lies on the top edge.
        let offset = { X = connectX; Y = height; Z = 0.0<_> }
        let outerBoxSize = { X = leafWidth * conversionFactor; Y = height; Z = 0.0<_> }
        LayoutBox.createLeaf outerBoxSize connectX personId offset

    let private attachParentsToDescendants
        (parentLeafBox: LayoutBox<ud>)
        (coParentAndChildGroupBoxes: (LayoutBox<ud> * LayoutBox<ld> seq)[])
        : LayoutBox<ld> =

        let combineWithParentBox (lineageBoxes: LayoutBox<ld>[]) : LayoutBox<ld> =
            let descendantsBox = lineageBoxes |> LayoutBox.attachHorizontally

            let parentConnectXOffset =
                if coParentAndChildGroupBoxes.Length % 2 = 0 then
                    0.0<ud>
                else
                    -coparentWidth / 2.0 * wd2ud

            // This could be negative if the descendants box is narrow (e.g. -- if there
            // is only one spouse and one child). In that case, the width of the resulting
            // box will be expanded accordingly by attachVertically.
            let parentLeftEdge =
                translateToLower (parentConnectXOffset - parentLeafBox.Size.X / 2.0) descendantsBox.ConnectX

            LayoutBox.attachVertically true parentLeftEdge descendantsBox parentLeafBox

        let createLineageBox i (coParentLeafBox, unattachedChildBoxes) =
            let childGroupBox =
                unattachedChildBoxes |> Array.ofSeq |> LayoutBox.attachHorizontally

            let direction =
                if i < coParentAndChildGroupBoxes.Length / 2 then
                    -1.0
                else
                    1.0

            // We need to account for the horizontal size of the coparent box in these calculations.
            let coParentOffsetX =
                translateToLower
                    (-coParentLeafBox.Size.X / 2.0)
                    (childGroupBox.ConnectX + direction * coparentWidth / 2.0 * wd2ld)

            LayoutBox.attachVertically false coParentOffsetX childGroupBox coParentLeafBox

        coParentAndChildGroupBoxes
        |> Array.mapi createLineageBox
        |> combineWithParentBox

    let private anchorRootBoxes rootBoxes =
        let rootBox = LayoutBox.attachHorizontally rootBoxes

        rootBox
        |> LayoutBox.setPosition
            { origin with
                X = -rootBox.ConnectX - coparentWidth * wd2ld / 2.0
                Y = -rootBox.Size.Y }

    let rec private calculateLayoutGrid focusedWilp familyGraph =
        let visitLeaf = leafBox wd2ld 0.0<ld>
        let visitParent = leafBox wd2ud 0.0<ud>
        let visitCoParent = leafBox wd2ud familyHeight

        familyGraph
        |> visitWilpForest focusedWilp visitLeaf visitParent visitCoParent attachParentsToDescendants comparePeople
        |> Array.ofSeq
        |> anchorRootBoxes

    let layoutGraph familyGraph focusedWilp =
        let place (personId, { X = x; Y = y; Z = z }: Vector<ld>) =
            let person = familyGraph |> findPerson personId

            { Id = NodeId personId.AsInt
              RenderedInWilp = focusedWilp
              Position = 0.0, 0.0, 0.0
              TargetPosition = float x, float y, float z
              IsAnimating = true
              Person = person }

        familyGraph |> calculateLayoutGrid focusedWilp |> Seq.map place

    let extractFamilies familyGraph nodes =
        let nodesByPersonInWilp =
            nodes
            |> Seq.map (fun node -> (node.Person.Id.AsInt, node.RenderedInWilp), node.Id)
            |> Map.ofSeq

        // Each Person appears at most once in a rendered Wilp, so this mapping is guaranteed to be unique.
        let personIdToNodeId wilp (personId: PersonId) =
            nodesByPersonInWilp |> Map.tryFind (personId.AsInt, wilp)

        let huwilpToRender = nodes |> Seq.map _.RenderedInWilp |> Seq.distinct

        seq {
            for rel in coparents familyGraph do
                let childrenOfMother = findChildren rel.Mother familyGraph
                let childrenOfFather = findChildren rel.Father familyGraph
                let childrenOfBoth = Set.intersect childrenOfMother childrenOfFather |> Set.toList

                for wilp in huwilpToRender do
                    let mapId = personIdToNodeId wilp

                    match mapId rel.Mother, mapId rel.Father, childrenOfBoth |> List.choose mapId with
                    | Some motherId, Some fatherId, (_ :: _ as childrenIds) ->
                        yield { Parents = motherId, fatherId; Children = childrenIds }
                    | _ -> () // Nothing to render since we need both parents and at least one child.
        }

// Functionality to initialize the core ViewModel data structures in an interface for easier consumption from TypeScript.
type IGraphViewFactory =
    abstract ExtractFamilies: FamilyGraph -> seq<TreeNode> -> seq<RenderedFamily>
    abstract FirstWilp: FamilyGraph -> WilpName
    abstract LayoutGraph: FamilyGraph -> WilpName -> seq<TreeNode>
    abstract LoadGraph: unit -> FamilyGraph

type GraphViewFactory() =
    interface IGraphViewFactory with
        member _.ExtractFamilies familyGraph nodes = Scene.extractFamilies familyGraph nodes

        member _.FirstWilp familyGraph = familyGraph |> huwilp |> Seq.head // ASSUMPTION: At least one Wilp is represented in the input data.

        member _.LayoutGraph familyGraph focusedWilp =
            Scene.layoutGraph familyGraph focusedWilp

        member _.LoadGraph() = createFamilyGraph peopleAndParents
