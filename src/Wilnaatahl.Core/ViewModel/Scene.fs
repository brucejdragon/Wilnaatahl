namespace Wilnaatahl.ViewModel

open Wilnaatahl.Model
open Wilnaatahl.Model.FamilyGraph
open Wilnaatahl.ViewModel.LayoutBox

/// This is a handy data structure for rendering the connectors between members of an
/// immediate family.
type RenderedFamily = { Parents: NodeId * NodeId; Children: NodeId list }

module Scene =
    let private defaultXSpacing = 1.95<w>
    let private defaultYSpacing = 2.0<w>
    let private defaultZSpacing = 0.0<w>
    let private origin = { X = 0.0<w>; Y = 0.0<w>; Z = 0.0<w> }

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

    let private leafBox<[<Measure>] 'u> (spacing: LayoutVector<'u>) height personId =
        let leafWidth = spacing.X
        let connectX = leafWidth / 2.0

        // When height = 0, this is effectively a 1-dimensional line in 3D space, but that's ok and
        // it makes the layout math easier. With any height, the Person shape centerpoint lies on the top edge.
        let offset = { X = connectX; Y = height; Z = 0.0<_> }
        let outerBoxSize = { X = leafWidth; Y = height; Z = 0.0<_> }
        createLeaf outerBoxSize connectX personId offset

    let private attachParentsToDescendants
        (spacing: LayoutVector<w>)
        (parentLeafBox: LayoutBox<u>)
        (coParentAndChildGroupBoxes: (LayoutBox<u> * LayoutBox<w> seq)[])
        : LayoutBox<w> =

        let familyCount = coParentAndChildGroupBoxes.Length

        let attachCoParentAndChildGroupBoxes i (coParentLeafBox: LayoutBox<u>, unattachedChildBoxes: LayoutBox<w> seq) =
            let coparentWidth = spacing.X * w2l

            let childGroupBox =
                unattachedChildBoxes |> Array.ofSeq |> attachHorizontally |> reframe w2l

            let direction = if i < familyCount / 2 then -1.0 else 1.0

            // We need to account for the horizontal size of the coparent box in these calculations.
            let coParentOffsetX =
                childGroupBox.ConnectX + direction * coparentWidth / 2.0
                - u2l * coParentLeafBox.Size.X / 2.0

            coParentLeafBox
            |> attachAbove childGroupBox { UseUpperConnectX = false; UpperOffset = coParentOffsetX }

        let descendantsBox =
            coParentAndChildGroupBoxes
            |> Array.mapi attachCoParentAndChildGroupBoxes
            |> attachHorizontally
            |> reframe w2l

        let parentConnectXOffset =
            let coparentWidth = spacing.X * w2l

            if familyCount % 2 = 0 then 0.0<l> else -coparentWidth / 2.0

        // This could be negative if the descendants box is narrow (e.g. -- if there
        // is only one spouse and one child). In that case, the width of the resulting
        // box will be expanded accordingly by attachAbove.
        let parentLeftEdge =
            descendantsBox.ConnectX + parentConnectXOffset
            - u2l * parentLeafBox.Size.X / 2.0

        parentLeafBox
        |> attachAbove descendantsBox { UseUpperConnectX = true; UpperOffset = parentLeftEdge }

    let private anchorRootBoxes spacing rootBoxes =
        let coparentWidth = spacing.X
        let rootBox = attachHorizontally rootBoxes

        rootBox
        |> setPosition {
            origin with
                X = -rootBox.ConnectX - coparentWidth / 2.0
                Y = -rootBox.Size.Y
        }

    let private calculateLayoutBoxes spacing focusedWilp familyGraph =
        let upperSpacing = spacing |> LayoutVector.reframe w2u

        let visitLeaf = leafBox spacing 0.0<w>
        let visitParent = leafBox upperSpacing 0.0<u>
        let visitCoParent = leafBox upperSpacing upperSpacing.Y
        let visitFamilies = attachParentsToDescendants spacing

        familyGraph
        |> visitWilpForest focusedWilp visitLeaf visitParent visitCoParent visitFamilies comparePeople
        |> Array.ofSeq
        |> anchorRootBoxes spacing

    let layoutGraph focusedWilp familyGraph =
        let spacing = { X = defaultXSpacing; Y = defaultYSpacing; Z = defaultZSpacing }

        let place (personId, { X = x; Y = y; Z = z }) =
            let person = familyGraph |> findPerson personId

            {
                Id = NodeId personId.AsInt
                RenderedInWilp = focusedWilp
                Position = 0.0, 0.0, 0.0
                TargetPosition = float x, float y, float z
                IsAnimating = true
                Person = person
            }

        familyGraph |> calculateLayoutBoxes spacing focusedWilp |> Seq.map place

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
