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
    type Vec3 =
        { X: float
          Y: float
          Z: float }

        static member inline (+)(lhs, rhs) =
            { X = lhs.X + rhs.X; Y = lhs.Y + rhs.Y; Z = lhs.Z + rhs.Z }

    type private GridBox =
        { Size: Vec3
          ConnectX: float
          FollowedBy: (GridBox * Vec3)[]
          SetPosition: Vec3 -> (PersonId * Vec3) list }

    module private GridBox =
        let create size connectX setPosition =
            { Size = size
              ConnectX = connectX
              FollowedBy = [||]
              SetPosition = setPosition }

        let rec setPosition pos box =
            seq {
                yield! box.SetPosition pos

                yield!
                    box.FollowedBy
                    |> Array.map (fun (followerBox, offset) -> followerBox |> setPosition (pos + offset))
                    |> Seq.concat
            }

        /// Creates a new box followed by all the given boxes that lays them out horizontally,
        /// in the order given from left to right (lower to highers X co-ordinate), aligned to
        /// the highest Y co-ordinate of the tallest box and sized to the deepest Z size.
        let attachHorizontally (boxes: GridBox[]) =
            let combineConnectXs (boxes: GridBox[]) =
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
                    (box, { X = distanceToTheLeft; Y = size.Y - box.Size.Y; Z = 0.0 }), distanceToTheLeft + box.Size.X

                let followers, _ = boxes |> Array.mapFold followNext 0

                { Size = size
                  ConnectX = connectX
                  FollowedBy = followers
                  SetPosition = fun _ -> [] }

        /// Attaches two boxes vertically, taking into account possible skew on the X axis.
        /// The given offset is the position of the upper box relative to the lower on the X axis.
        /// If the value is 0, the two are aligned on the left edge. If it's negative, the upper box
        /// extends to the left of the lower one, and its left edge will be the zero X point of the new
        /// box. If it's positive, the lower box extends to the left of the upper one, and its left edge
        /// will be the zero X point of the new box.
        let attachVertically upperOffset lowerBox upperBox =
            // Take into account skew, so if one box doesn't completely encompass the other's width,
            // we get the right overall width. Since the offset is relative to the lower box, it should
            // add to the upper box when positive, and add to the lower box when negative.
            let adjustedLowerBoxWidth = max lowerBox.Size.X (-upperOffset + lowerBox.Size.X)
            let adjustedHigherBoxWidth = max upperBox.Size.X (upperOffset + upperBox.Size.X)

            let size =
                { X = max adjustedLowerBoxWidth adjustedHigherBoxWidth
                  Y = lowerBox.Size.Y + upperBox.Size.Y
                  Z = max lowerBox.Size.Z upperBox.Size.Z }

            { Size = size
              ConnectX = upperBox.ConnectX
              FollowedBy =
                [| lowerBox, { X = max -upperOffset 0.0; Y = 0.0; Z = 0.0 }
                   upperBox, { X = max upperOffset 0.0; Y = lowerBox.Size.Y; Z = 0.0 } |]
              SetPosition = fun _ -> [] }

    let private leafWidth = 1.95
    let private familyHeight = 2.0
    let private coparentWidth = 1.9
    let private origin = { X = 0.0; Y = 0.0; Z = 0.0 }

    // Used to sort people for layout by comparing Date of Birth (DoB), names if DoB is missing, or
    // person ID if names are missing.
    let private comparePeople familyGraph personId1 personId2 =
        let person1, person2 =
            familyGraph |> findPerson personId1, familyGraph |> findPerson personId2

        match person1.DateOfBirth, person2.DateOfBirth with
        | Some dob1, Some dob2 ->
            // Fable doesn't appear to support DateOnly.CompareTo.
            if dob1 < dob2 then -1
            elif dob1 > dob2 then 1
            else 0
        | Some _, None -> 1
        | None, Some _ -> -1
        | None, None ->
            match person1.Label, person2.Label with
            | Some label1, Some label2 -> label1.CompareTo label2
            | Some _, None -> 1
            | None, Some _ -> -1
            | None, None -> personId1.AsInt - personId2.AsInt

    let private leafBox personId _ =
        let connectX = leafWidth / 2.0

        let setPosition pos =
            [ personId, { pos with X = connectX + pos.X } ]

        // This is effectively a 1-dimensional line in 3D space, but that's ok and
        // it makes the layout math easier.
        GridBox.create { X = leafWidth; Y = 0.0; Z = 0.0 } connectX setPosition

    let private attachParentsToDescendants parentId coParentsToUnattachedChildBoxes familyGraph =
        let measureForParentBox (childGroupBoxes: GridBox[]) descendantsBox =
            // NOTE: In this function, the parent X co-ordinates are relative to descendantsBox.
            if childGroupBoxes.Length = 1 then
                // We ignore the node sizes since  we connect center-to-center
                let parentBoxWidth = coparentWidth
                let parentX = descendantsBox.ConnectX - coparentWidth / 2.0
                parentBoxWidth, parentX
            else
                let leftChildGroupBox, rightChildGroupBox =
                    childGroupBoxes[0], childGroupBoxes[childGroupBoxes.Length - 1]

                let leftCoParentX = leftChildGroupBox.ConnectX - coparentWidth / 2.0

                let rightCoParentX =
                    descendantsBox.Size.X - rightChildGroupBox.Size.X
                    + rightChildGroupBox.ConnectX
                    + coparentWidth / 2.0

                let parentBoxWidth = rightCoParentX - leftCoParentX
                parentBoxWidth, leftCoParentX

        let coParentMap =
            coParentsToUnattachedChildBoxes
            |> Map.map (fun _ unattachedChildBoxes -> unattachedChildBoxes |> Array.ofSeq |> GridBox.attachHorizontally)

        let childGroupBoxes = coParentMap |> Map.values |> Array.ofSeq
        let descendantsBox = GridBox.attachHorizontally childGroupBoxes

        let parentBoxWidth, parentBoxOffset =
            measureForParentBox childGroupBoxes descendantsBox

        let parentBoxSize =
            { descendantsBox.Size with X = parentBoxWidth; Y = familyHeight }

        let parentConnectX =
            if childGroupBoxes.Length % 2 <> 0 then
                descendantsBox.ConnectX - coparentWidth / 2.0
            else
                descendantsBox.ConnectX

        let setPosition pos =
            [ yield
                  parentId,
                  { X = pos.X + parentConnectX
                    Y = pos.Y + parentBoxSize.Y
                    Z = pos.Z + parentBoxSize.Z }

              let placeCoParent distanceToTheLeft (i, coParentId) =
                  let childGroupBox = coParentMap[coParentId]

                  let xOffset =
                      if i < coParentMap.Count / 2 then
                          -coparentWidth / 2.0
                      else
                          coparentWidth / 2.0

                  let relativeX = distanceToTheLeft + childGroupBox.ConnectX + xOffset

                  (coParentId,
                   { X = pos.X + relativeX
                     Y = pos.Y + parentBoxSize.Y
                     Z = pos.Z + parentBoxSize.Z }),
                  distanceToTheLeft + relativeX

              let coParentIds =
                  coParentMap
                  |> Map.keys
                  |> Array.ofSeq
                  |> Seq.sortWith (comparePeople familyGraph) // TODO: Find a way to sort descendants too!

              let coParentsWithCoordinates, _ =
                  coParentIds |> Seq.indexed |> Seq.mapFold placeCoParent 0

              yield! coParentsWithCoordinates ]

        let parentBox = GridBox.create parentBoxSize parentConnectX setPosition
        GridBox.attachVertically parentBoxOffset descendantsBox parentBox

    let private anchorRootBoxes rootBoxes =
        let topLevelBox = GridBox.attachHorizontally rootBoxes

        let rootSetPosition innerSetPos pos =
            let rootPos = { pos with X = -topLevelBox.ConnectX; Y = -topLevelBox.Size.Y }
            innerSetPos rootPos

        let rootBox =
            { topLevelBox with
                SetPosition = rootSetPosition topLevelBox.SetPosition }

        rootBox.SetPosition origin

    let rec private calculateLayoutGrid focusedWilp familyGraph =
        familyGraph
        |> visitWilpForest focusedWilp leafBox attachParentsToDescendants
        |> Array.ofSeq
        |> anchorRootBoxes

    let layoutGraph familyGraph focusedWilp =
        let place (personId, { X = x; Y = y; Z = z }) =
            let person = familyGraph |> findPerson personId

            { Id = NodeId personId.AsInt
              RenderedInWilp = focusedWilp
              Position = 0.0, 0.0, 0.0
              TargetPosition = x, y, z
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
