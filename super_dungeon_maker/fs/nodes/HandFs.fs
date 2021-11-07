namespace super_dungeon_maker

open Godot
open GDUtils

type HandState =
    | Nothing
    | SelectedBlock of int
    | SelectedEnemy of int

type ToDisplay =
    | Blocks
    | Enemies
    | Misc

type HandFs() as this =
    inherit Control()

    let mutable handState = Nothing

    let mutable drawnHand: List<TextureButton> = []

    let dungeonMap = this.getNode<TileMap> "../DungeonMap"
    let itemMap = this.getNode<TileMap> "../EnemyMap"

    let showBlocks = this.getNode<Button> "./BlocksButton"
    let showEnemies = this.getNode<Button> "./EnemiesButton"
    let showMisc = this.getNode<Button> "./MiscButton"

    let mutable toShowState = ToDisplay.Blocks

    let enableCard () =
        match handState with
        | Nothing -> ()
        | SelectedBlock x
        | SelectedEnemy x ->
            let card = drawnHand.[x]
            card.Disabled <- false

    let mutable drawnMap =
        System.Collections.Generic.Dictionary<(int * int), Block>()

    let blockHand =
        Hand.newHand [ 2, BlockKinds.UpAndDown
                       2, BlockKinds.LeftAndRight
                       2, BlockKinds.AllOpen ]

    let enemyHand =
        Hand.newHand [ 3, EnemyKinds.BasicEnemy ]

    let miscHand = Hand.newHand [ 1, Misc.Start ]

    let showMaker (a: Lazy<Button>) (b: Lazy<Button>) (c: Lazy<Button>) d =
        match toShowState with
        | ToDisplay.Blocks -> Hand.turnHandOff blockHand
        | ToDisplay.Enemies -> Hand.turnHandOff enemyHand
        | ToDisplay.Misc -> Hand.turnHandOff miscHand

        a.Value.Disabled <- true
        b.Value.Disabled <- false
        c.Value.Disabled <- false
        toShowState <- d

    member val UpdateCostBar: int -> unit = ignore with get, set


    member public _.GetMap() = drawnMap


    member this.ClickedItem(clicked: int) =
        let x =
            match toShowState with
            | ToDisplay.Enemies -> Hand.ClickedOn enemyHand
            | ToDisplay.Blocks -> Hand.ClickedOn blockHand
            | ToDisplay.Misc -> Hand.ClickedOn miscHand

        x clicked

    member public this.DrawHand() =
        let drawer =
            match toShowState with
            | ToDisplay.Enemies -> Hand.DrawHand enemyHand (EnemyKinds.toPicture >> loadEnemies)
            | ToDisplay.Blocks -> Hand.DrawHand blockHand (BlockKinds.toPicture >> loadBlocks)
            | ToDisplay.Misc -> Hand.DrawHand miscHand (Misc.toPicture >> loadMisc)

        drawer (fun x -> ("pressed", this, "ClickedItem", (SParam x))) this

    override this._Ready() =
        showMaker showBlocks showEnemies showMisc ToDisplay.Blocks
        this.DrawHand()

    override this._Input event =
        let position =
            match event with
            | :? InputEventMouseButton as button ->
                if button.ButtonIndex = (int) ButtonList.Left then
                    if button.Position.y > this.RectGlobalPosition.y then
                        None
                    else
                        let worldPosition =
                            dungeonMap.Value.WorldToMap button.Position

                        let roomCoordinateRounded =
                            (worldPosition.x
                             |> Mathf.Round
                             |> System.Convert.ToInt32,
                             worldPosition.y
                             |> Mathf.Round
                             |> System.Convert.ToInt32)

                        Some(button.Position, roomCoordinateRounded, dungeonMap.Value.WorldToMap button.Position)
                else
                    None
            | x -> None

        let res =
            match position with
            | None -> None
            | Some (mousePos, roomCoordinateRounded, roomCoordinate) ->
                let toInsert =
                    match toShowState with
                    | ToDisplay.Blocks -> Ok Blocks
                    | ToDisplay.Enemies ->
                        let insertEnemy =
                            enemyHand |> Hand.getSelected |> fun (_, v) -> v

                        Result.Error(Items.Enemy insertEnemy, insertEnemy |> EnemyKinds.toPicture |> loadEnemies)
                    | ToDisplay.Misc ->
                        let insertMisc =
                            miscHand |> Hand.getSelected |> fun (_, v) -> v

                        Result.Error(Items.Misc insertMisc, insertMisc |> Misc.toPicture |> loadMisc)

                match toInsert with
                | Ok _ ->
                    let insertBlockKind =
                        blockHand |> Hand.getSelected |> fun (_, v) -> v

                    let insertBlock = insertBlockKind |> BlockKinds.toBlock

                    //if there are no neighbours at all, then false
                    //if one or more of the neigbours don't have openings that line up with the openings of this block, then false
                    //else, true
                    let hasNeighbour =
                        drawnMap
                        |> Seq.filter (fun x -> x.Key <> roomCoordinateRounded)
                        |> Seq.filter (fun x -> IsNeighbour x.Key roomCoordinateRounded)
                        |> Seq.map (fun x -> (Direction.toDirection x.Key roomCoordinateRounded), x.Value)
                        |> Seq.map (fun (dir, value) -> Block.fitBlocks value insertBlock dir)
                        |> Set
                        |> (=) (Set [ true ])

                    if drawnMap.Count <= 0
                       || hasNeighbour
                          && roomCoordinateRounded
                             |> drawnMap.ContainsKey
                             |> not then
                        drawnMap.Add(roomCoordinateRounded, insertBlock)

                        let texture =
                            insertBlockKind
                            |> BlockKinds.toPicture
                            |> loadBlocks

                        let location =
                            dungeonMap.Value.MapToWorld(roomCoordinate)

                        Some(texture, location, dungeonMap.Value.CellSize)
                    else
                        None
                | Result.Error (toInsert, texture) ->
                    if drawnMap.ContainsKey roomCoordinateRounded then
                        let worldEnemyLocation = itemMap.Value.WorldToMap mousePos

                        let tileEnemyLocation =
                            ((worldEnemyLocation.x
                              |> Mathf.Round
                              |> System.Convert.ToInt32) % 4,
                             (worldEnemyLocation.y
                              |> Mathf.Round
                              |> System.Convert.ToInt32) % 4)

                        let enemies = drawnMap.[roomCoordinateRounded].Items

                        if tileEnemyLocation |> enemies.ContainsKey |> not then
                            enemies.Add(tileEnemyLocation, toInsert)


                            let location =
                                itemMap.Value.MapToWorld worldEnemyLocation

                            Some(texture, location, itemMap.Value.CellSize)
                        else
                            None
                    else
                        None

        match res with
        | None -> ()
        | Some (texture, location, size) ->
            let sprite = new TextureRect()
            sprite.RectGlobalPosition <- location
            sprite.RectSize <- size
            sprite.Expand <- true
            sprite.Texture <- texture
            handState <- Nothing
            this.GetParent().AddChild(sprite)
            this.DrawHand()

        let z =
            drawnMap.Values
            |> Seq.sumBy (fun x ->
                -5
                + (x.Items.Values
                   |> Seq.sumBy (function
                       | Items.Enemy x -> EnemyKinds.toCost x
                       | Items.Misc x -> Misc.toCost x)))

        this.UpdateCostBar z

    member this.HasPlacedStart() =
        drawnMap.Values
        |> Seq.collect (fun x -> x.Items.Values)
        |> Seq.choose (function
            | Items.Enemy x -> None
            | Items.Misc x -> Some x)
        |> Seq.contains Misc.Start

    member this.OnBlocksPressed() =
        showMaker showBlocks showEnemies showMisc ToDisplay.Blocks
        this.DrawHand()

    member this.OnEnemiesPressed() =
        showMaker showEnemies showBlocks showMisc ToDisplay.Enemies
        this.DrawHand()

    member this.OnMiscPressed() =
        showMaker showMisc showBlocks showEnemies ToDisplay.Misc
        this.DrawHand()
