module Wilnaatahl.Tests.ViewModel.LayoutBoxTests

open Xunit
open Swensen.Unquote
open Wilnaatahl.Model
open Wilnaatahl.ViewModel
open Wilnaatahl.ViewModel.LayoutBox
open Wilnaatahl.Tests

let private vecZeroW = LayoutVector<w>.Zero
let private vecZeroU = LayoutVector<u>.Zero
let private vecZeroL = LayoutVector<l>.Zero

[<Fact>]
let ``LayoutVector addition works`` () =
    let v1 = { X = 1.0<w>; Y = 2.0<w>; Z = 3.0<w> }
    let v2 = { X = 4.0<w>; Y = 5.0<w>; Z = 6.0<w> }
    let expected = { X = 5.0<w>; Y = 7.0<w>; Z = 9.0<w> }
    let actual = v1 + v2
    actual =! expected

[<Fact>]
let ``LayoutVector.reframe changes units correctly`` () =
    let v = { X = 2.0<w>; Y = 3.0<w>; Z = 4.0<w> }
    let expected = { X = 20.0<u>; Y = 30.0<u>; Z = 40.0<u> }
    let actual = LayoutVector.reframe 10.0<u / w> v
    actual =! expected

[<Fact>]
let ``createLeaf creates correct LayoutBox`` () =
    let size = { X = 1.0<w>; Y = 2.0<w>; Z = 3.0<w> }
    let connectX = 0.5<w>
    let personId = PersonId 42
    let offset = { X = 0.1<w>; Y = 0.2<w>; Z = 0.3<w> }

    let expected = {
        Size = size
        ConnectX = connectX
        Payload = LeafBox(personId, offset)
    }

    let actual = createLeaf size connectX personId offset
    actual =! expected

[<Fact>]
let ``createComposite creates correct LayoutBox`` () =
    let size = { X = 1.0<w>; Y = 2.0<w>; Z = 3.0<w> }
    let connectX = 0.5<w>

    let composite = { TopLeftWidth = 0.1<w>; TopRightWidth = 0.2<w>; Followers = [] }
    let expected = { Size = size; ConnectX = connectX; Payload = CompositeBox composite }

    let actual = createComposite size connectX composite
    actual =! expected

[<Fact>]
let ``reframe changes LayoutBox units recursively`` () =
    let size = { X = 1.0<w>; Y = 2.0<w>; Z = 3.0<w> }
    let connectX = 0.5<w>
    let offset = { X = 0.1<w>; Y = 0.2<w>; Z = 0.3<w> }
    let leaf = createLeaf size connectX (PersonId 1) offset

    let composite =
        createComposite size connectX {
            TopLeftWidth = 0.1<w>
            TopRightWidth = 0.2<w>
            Followers = [ leaf, vecZeroW ]
        }

    let expected =
        let size = { X = 2.0<u>; Y = 4.0<u>; Z = 6.0<u> }

        let followers =
            let leaf =
                createLeaf size 1.0<u> (PersonId 1) { X = 0.2<u>; Y = 0.4<u>; Z = 0.6<u> }

            [ leaf, vecZeroU ]

        let composite = {
            TopLeftWidth = 0.2<u>
            TopRightWidth = 0.4<u>
            Followers = followers
        }

        { Size = size; ConnectX = 1.0<u>; Payload = CompositeBox composite }

    let actual = reframe 2.0<u / w> composite
    actual =! expected

[<Fact>]
let ``visit visits all boxes and passes correct positions`` () =
    let size = { X = 1.0<w>; Y = 2.0<w>; Z = 3.0<w> }
    let connectX = 0.5<w>
    let offset = { X = 0.1<w>; Y = 0.2<w>; Z = 0.3<w> }
    let leaf1 = createLeaf size connectX (PersonId 1) offset
    let leaf2 = createLeaf size connectX (PersonId 2) offset

    let box =
        createComposite size connectX {
            TopLeftWidth = 0.1<w>
            TopRightWidth = 0.2<w>
            Followers = [
                leaf1, { X = 1.0<w>; Y = 2.0<w>; Z = 3.0<w> }
                leaf2, { X = 4.0<w>; Y = 5.0<w>; Z = 6.0<w> }
            ]
        }

    let expected = [
        PersonId 1, { X = 1.1<w>; Y = 2.2<w>; Z = 3.3<w> }
        PersonId 2, { X = 4.1<w>; Y = 5.2<w>; Z = 6.3<w> }
    ]

    let actual = TestUtils.setPositions (vecZeroW, box) |> Seq.toList

    // Compare as sets (order doesn't matter)
    Set.ofList actual =! Set.ofList expected

[<Fact>]
let ``attachHorizontally combines leaf boxes correctly`` () =
    let size = { X = 2.0<w>; Y = 1.0<w>; Z = 1.0<w> }
    let connectX = 1.0<w>

    let leaf1 = createLeaf size connectX (PersonId 1) vecZeroW
    let leaf2 = createLeaf size connectX (PersonId 2) vecZeroW

    let expected =
        let followers = [ leaf1, vecZeroW; leaf2, { X = 2.0<w>; Y = 0.0<w>; Z = 0.0<w> } ]

        let composite = {
            TopLeftWidth = 0.0<w>
            TopRightWidth = 0.0<w>
            Followers = followers
        }

        createComposite { X = 4.0<w>; Y = 1.0<w>; Z = 1.0<w> } 2.0<w> composite

    let actual = attachHorizontally [| leaf1; leaf2 |]
    actual =! expected

[<Fact>]
let ``attachHorizontally combines two composite boxes correctly`` () =
    let composite1 =
        let size = { X = 1.0<w>; Y = 1.0<w>; Z = 2.0<w> }
        let leaf = createLeaf size 0.5<w> (PersonId 1) vecZeroW

        createComposite size 0.5<w> {
            TopLeftWidth = 0.5<w>
            TopRightWidth = 0.25<w>
            Followers = [ leaf, vecZeroW ]
        }

    let composite2 =
        let size = { X = 2.0<w>; Y = 3.0<w>; Z = 1.0<w> }
        let leaf = createLeaf size 1.0<w> (PersonId 2) vecZeroW

        createComposite size 1.0<w> {
            TopLeftWidth = 0.25<w>
            TopRightWidth = 0.5<w>
            Followers = [ leaf, vecZeroW ]
        }

    let expected =
        let followers = [
            composite1, { X = 0.0<w>; Y = 2.0<w>; Z = 0.0<w> }
            composite2, { X = 1.0<w>; Y = 0.0<w>; Z = 0.0<w> }
        ]

        let composite = {
            TopLeftWidth = 0.5<w>
            TopRightWidth = 0.5<w>
            Followers = followers
        }

        createComposite { X = 3.0<w>; Y = 3.0<w>; Z = 2.0<w> } 1.25<w> composite

    let actual = attachHorizontally [| composite1; composite2 |]
    actual =! expected

[<Theory>]
[<InlineData(0.0<w>, 3.0<w>, 1.25<w>, 1.0<w>)>]
[<InlineData(1e-9<w>, 3.0<w>, 1.25<w>, 1.0<w>)>]
[<InlineData(0.5<w>, 2.5<w>, 1.0<w>, 0.5<w>)>]
let ``attachHorizontally: Composite on left, Leaf on right, TopRightWidth zero or nonzero``
    (topRightWidth: float<w>)
    (expectedSizeX: float<w>)
    (expectedConnectX: float<w>)
    (leafOffsetX: float<w>)
    =
    let composite =
        let size = { X = 1.0<w>; Y = 1.0<w>; Z = 1.0<w> }
        let leaf = createLeaf size 0.5<w> (PersonId 1) vecZeroW

        createComposite size 0.5<w> {
            TopLeftWidth = 0.0<w>
            TopRightWidth = topRightWidth
            Followers = [ leaf, vecZeroW ]
        }

    let leaf =
        createLeaf { X = 2.0<w>; Y = 1.0<w>; Z = 1.0<w> } 1.0<w> (PersonId 2) vecZeroW

    let expected =
        let followers = [ composite, vecZeroW; leaf, { X = leafOffsetX; Y = 0.0<w>; Z = 0.0<w> } ]

        let composite = {
            TopLeftWidth = 0.0<w>
            TopRightWidth = 0.0<w>
            Followers = followers
        }

        createComposite { X = expectedSizeX; Y = 1.0<w>; Z = 1.0<w> } expectedConnectX composite

    let actual = attachHorizontally [| composite; leaf |]
    actual =! expected

[<Theory>]
[<InlineData(0.0<w>, 3.0<w>, 1.25<w>, 1.0<w>)>]
[<InlineData(1e-9<w>, 3.0<w>, 1.25<w>, 1.0<w>)>]
[<InlineData(0.5<w>, 2.5<w>, 1.0<w>, 0.5<w>)>]
let ``attachHorizontally: Leaf on left, Composite on right, TopLeftWidth zero or nonzero``
    (topLeftWidth: float<w>)
    (expectedSizeX: float<w>)
    (expectedConnectX: float<w>)
    (compositeOffsetX: float<w>)
    =
    let leaf =
        createLeaf { X = 1.0<w>; Y = 1.0<w>; Z = 1.0<w> } 0.5<w> (PersonId 1) vecZeroW

    let composite =
        let size = { X = 2.0<w>; Y = 1.0<w>; Z = 1.0<w> }
        let leaf = createLeaf size 1.0<w> (PersonId 2) vecZeroW

        createComposite size 1.0<w> {
            TopLeftWidth = topLeftWidth
            TopRightWidth = 0.0<w>
            Followers = [ leaf, vecZeroW ]
        }

    let expected =
        let followers = [ leaf, vecZeroW; composite, { X = compositeOffsetX; Y = 0.0<w>; Z = 0.0<w> } ]

        let composite = {
            TopLeftWidth = 0.0<w>
            TopRightWidth = 0.0<w>
            Followers = followers
        }

        createComposite { X = expectedSizeX; Y = 1.0<w>; Z = 1.0<w> } expectedConnectX composite

    let actual = attachHorizontally [| leaf; composite |]
    actual =! expected

[<Fact>]
let ``attachHorizontally with odd number of boxes uses correct ConnectX calculation`` () =
    let leaf1 =
        createLeaf { X = 1.0<w>; Y = 1.0<w>; Z = 1.0<w> } 0.5<w> (PersonId 1) vecZeroW

    let leaf2 =
        createLeaf { X = 2.0<w>; Y = 1.0<w>; Z = 1.0<w> } 1.0<w> (PersonId 2) vecZeroW

    let leaf3 =
        createLeaf { X = 3.0<w>; Y = 1.0<w>; Z = 1.0<w> } 1.5<w> (PersonId 3) vecZeroW

    let expected =
        let followers = [
            leaf1, vecZeroW
            leaf2, { X = 1.0<w>; Y = 0.0<w>; Z = 0.0<w> }
            leaf3, { X = 3.0<w>; Y = 0.0<w>; Z = 0.0<w> }
        ]

        let composite = {
            TopLeftWidth = 0.0<w>
            TopRightWidth = 0.0<w>
            Followers = followers
        }

        createComposite { X = 6.0<w>; Y = 1.0<w>; Z = 1.0<w> } 2.0<w> composite

    let actual = attachHorizontally [| leaf1; leaf2; leaf3 |]
    actual =! expected

[<Fact>]
let ``attachHorizontally returns the same box when only one box is given`` () =
    let box =
        createLeaf { X = 2.0<w>; Y = 1.0<w>; Z = 1.0<w> } 1.0<w> (PersonId 1) vecZeroW

    let actual = attachHorizontally [| box |]
    test <@ LanguagePrimitives.PhysicalEquality actual box @>

[<Theory>]
[<InlineData(true, 2.5<w>)>]
[<InlineData(false, 1.0<w>)>]
let ``attachAbove attaches boxes vertically and produces expected box`` useUpperConnectX expectedConnectX =
    let lower =
        createLeaf { X = 2.0<l>; Y = 1.0<l>; Z = 1.0<l> } 1.0<l> (PersonId 1) vecZeroL

    let upper =
        createLeaf { X = 3.0<u>; Y = 2.0<u>; Z = 1.0<u> } 1.5<u> (PersonId 2) vecZeroU

    let options = { UseUpperConnectX = useUpperConnectX; UpperOffset = 1.0<l> }

    let expected =
        let lower' = reframe 1.0<w / l> lower
        let upper' = reframe 1.0<w / u> upper

        let followers = [ lower', vecZeroW; upper', { X = 1.0<w>; Y = 1.0<w>; Z = 0.0<w> } ]

        let composite = {
            TopLeftWidth = 1.0<w>
            TopRightWidth = 0.0<w>
            Followers = followers
        }

        createComposite { X = 4.0<w>; Y = 3.0<w>; Z = 1.0<w> } expectedConnectX composite

    let actual = upper |> attachAbove lower options
    actual =! expected

[<Theory>]
[<InlineData(0.0<u>, 0.0<w>)>]
[<InlineData(1e-9<u>, 0.0<w>)>]
[<InlineData(1.0<u>, 1.0<w>)>]
let ``attachAbove with lower Composite and upper height zero or non-zero affects corners``
    (upperHeight: float<u>)
    (expectedTopLeftWidth: float<w>)
    =
    let lower =
        let size = { X = 2.0<l>; Y = 1.0<l>; Z = 1.0<l> }
        let lowerLeaf = createLeaf size 1.0<l> (PersonId 1) vecZeroL

        createComposite size 1.0<l> {
            TopLeftWidth = 0.0<l>
            TopRightWidth = 0.0<l>
            Followers = [ lowerLeaf, vecZeroL ]
        }

    let upper =
        createLeaf { X = 3.0<u>; Y = upperHeight; Z = 1.0<u> } 1.5<u> (PersonId 2) {
            X = 0.0<u>
            Y = 0.0<u>
            Z = 0.0<u>
        }

    let options = { UseUpperConnectX = true; UpperOffset = 1.0<l> }
    let lower' = reframe 1.0<w / l> lower
    let upper' = reframe 1.0<w / u> upper

    let expected =
        let expectedSizeY =
            lower'.Size.Y + if upper'.Size.Y > nearZero then upper'.Size.Y else 0.0<w>

        let followers = [ lower', vecZeroW; upper', { X = 1.0<w>; Y = 1.0<w>; Z = 0.0<w> } ]

        let composite = {
            TopLeftWidth = expectedTopLeftWidth
            TopRightWidth = 0.0<w>
            Followers = followers
        }

        createComposite { X = 4.0<w>; Y = expectedSizeY; Z = 1.0<w> } 2.5<w> composite

    let actual = upper |> attachAbove lower options
    actual =! expected
