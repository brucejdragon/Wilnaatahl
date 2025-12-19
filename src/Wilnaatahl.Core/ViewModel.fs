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
    type private Vec3 = { X: float; Y: float; Z: float }

    type private GridBox =
        { Size: Vec3
          ConnectX: float
          SetPosition: Vec3 -> (PersonId * Vec3) seq }

    let private leafWidth = 1.95
    let private familyHeight = 2.0
    let private coparentWidth = 1.9
    let private origin = { X = 0.0; Y = 0.0; Z = 0.0 }

    let rec private leafBox personId =
        let connectX = leafWidth / 2.0

        // This is effectively a 1-dimensional line in 3D space, but that's ok and
        // it makes the layout math easier.
        { Size = { X = leafWidth; Y = 0.0; Z = 0.0 }
          ConnectX = connectX
          SetPosition = fun pos -> [ personId, { pos with X = connectX + pos.X } ] }

    let private combineConnectXs boxes =
        let b = boxes |> Array.ofSeq
        let boxCount = b.Length
        let middle = (boxCount - 1) / 2
        let distanceToLeft = b[0 .. middle - 1] |> Array.sumBy _.Size.X

        if boxCount % 2 <> 0 then
            distanceToLeft + b[middle].ConnectX
        else
            let left, right = b[middle], b[middle + 1]

            // Trust me, it works, I did the math.
            (2.0 * distanceToLeft + left.ConnectX + left.Size.X + right.ConnectX) / 2.0

    let rec private attachHorizontally (boxes: GridBox[]) =
        // Base case is that there is just one box, in which case we return it.
        if boxes.Length = 1 then
            boxes[0]
        else
            let size =
                { X = (boxes |> Seq.sumBy _.Size.X) + leafWidth // Add some horizontal buffer so lineages don't overlap.
                  Y = boxes |> Seq.map _.Size.Y |> Seq.max
                  Z = boxes |> Seq.map _.Size.Z |> Seq.max }

            let connectX = combineConnectXs boxes

            let setPosition pos =
                seq {
                    for i in [ 0 .. boxes.Length - 1 ] do
                        let box = boxes[i]
                        let distanceToTheLeft = leafWidth + (boxes[0 .. i - 1] |> Array.sumBy _.Size.X)

                        yield!
                            box.SetPosition
                                { pos with
                                    X = pos.X + distanceToTheLeft
                                    Y = pos.Y + size.Y - box.Size.Y }
                }

            { Size = size; ConnectX = connectX; SetPosition = setPosition }

    let rec private attachParentsToDescendants parentId spousesToUnattachedChildBoxes =
        let measureForParentBox (childGroupBoxes: GridBox[]) descendantsBox =
            if childGroupBoxes.Length = 1 then
                // We ignore the node sizes since  we connect center-to-center
                let parentBoxWidth = max descendantsBox.Size.X coparentWidth

                let leftOffset =
                    if descendantsBox.Size.X = parentBoxWidth then
                        0.0
                    else
                        coparentWidth / 2.0 - descendantsBox.ConnectX

                parentBoxWidth, leftOffset
            else
                let leftChildGroupBox, rightChildGroupBox =
                    childGroupBoxes[0], childGroupBoxes[childGroupBoxes.Length - 1]

                // Use leaf width to ensure buffer between parent boxes if descendant boxes are narrower.
                let leftOffset =
                    max (-1.0 * (leftChildGroupBox.ConnectX - coparentWidth / 2.0 - leafWidth / 2.0)) 0.0

                let rightOffset =
                    max
                        (rightChildGroupBox.ConnectX + coparentWidth / 2.0 + leafWidth / 2.0
                         - rightChildGroupBox.Size.X)
                        0.0

                descendantsBox.Size.X + leftOffset + rightOffset, leftOffset

        let spouseMap =
            spousesToUnattachedChildBoxes
            |> Map.map (fun _ unattachedChildBoxes -> unattachedChildBoxes |> Array.ofSeq |> attachHorizontally)

        let childGroupBoxes = spouseMap |> Map.values |> Array.ofSeq

        let descendantsBox = attachHorizontally childGroupBoxes
        let parentBoxWidth, leftOffset = measureForParentBox childGroupBoxes descendantsBox

        let size =
            { descendantsBox.Size with
                X = parentBoxWidth
                Y = descendantsBox.Size.Y + familyHeight }

        let centeredX = leftOffset + descendantsBox.ConnectX

        let parentX =
            if childGroupBoxes.Length % 2 <> 0 then
                centeredX - coparentWidth / 2.0
            else
                centeredX

        let setPosition pos =
            seq {
                yield parentId, { X = pos.X + parentX; Y = pos.Y + size.Y; Z = pos.Z + size.Z }

                let spouseIds = spouseMap |> Map.keys |> Array.ofSeq

                for i in 0 .. spouseIds.Length - 1 do
                    let spouseId = spouseIds[i]
                    let childGroupBox = spouseMap[spouseId]

                    let distanceToTheLeft =
                        spouseIds[0 .. i - 1]
                        |> Array.map (fun k -> Map.find k spouseMap)
                        |> Array.sumBy _.Size.X

                    let xOffset =
                        if i < spouseIds.Length / 2 then
                            -coparentWidth / 2.0
                        else
                            coparentWidth / 2.0

                    yield
                        spouseId,
                        { X = pos.X + distanceToTheLeft + childGroupBox.ConnectX + xOffset
                          Y = pos.Y + size.Y
                          Z = pos.Z + size.Z }

                yield! descendantsBox.SetPosition { pos with X = pos.X + leftOffset }
            }

        { Size = size; ConnectX = parentX; SetPosition = setPosition }

    let private anchorRootBoxes rootBoxes =
        let topLevelBox = attachHorizontally rootBoxes

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
