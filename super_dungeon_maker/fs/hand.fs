namespace super_dungeon_maker

open Godot

type Hand<'t>(cards) =
    member val public Cards: List<int * 't> = cards with get, set
    member val public DrawnCards: List<TextureButton> = [] with get, set
    member val public LastSelected: Option<int> = None with get, set

module Hand =
    let newHand a = Hand(a)

    let getSelected<'t> (hand: Hand<'t>) =
        let selected =
            hand.LastSelected |> Option.defaultValue 0

        hand.Cards.[selected]

    let ClickedOn<'t> (hand: Hand<'t>) on =
        if on < hand.DrawnCards.Length then
            match hand.LastSelected with
            | Some x -> hand.DrawnCards.[x].Disabled <- false
            | None -> ()

            hand.DrawnCards.[on].Disabled <- true
            hand.LastSelected <- Some on

    let private drawButton img key size =
        let button = new TextureButton()
        button.set_TextureNormal img
        button.Expand <- true
        button.RectSize <- size
        button.RectPosition <- Vector2(button.RectSize.x * float32 (key) + 5f, 0f)
        button

    let DrawHand<'a, 't when 't :> Object>
        (hand: Hand<'a>)
        getSprite
        (toConnect: int -> string * 't * string * Collections.Array)
        (addTo: Node)
        =
        for v in hand.DrawnCards do
            v.QueueFree()

        hand.DrawnCards <-
            hand.Cards
            |> Seq.mapi (fun key (_, v) ->
                let (img: Texture) = v |> getSprite
                let size = img.GetSize() * 5f
                let button = drawButton img key size

                let (signal, target, method, binds) = toConnect key

                (button.Connect(signal, target, method, binds))
                |> GDUtils.LogIgnore

                addTo.AddChild(button)
                button)
            |> Seq.toList

    let turnHandOff<'t> (oldHand: Hand<'t>) =
        for v in oldHand.DrawnCards do
            v.QueueFree()

        oldHand.LastSelected <- None
