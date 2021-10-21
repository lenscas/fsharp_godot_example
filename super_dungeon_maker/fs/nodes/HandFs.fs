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
    let enemyMap = this.getNode<TileMap> "../EnemyMap"

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
            | ToDisplay.Misc -> Hand.DrawHand miscHand (Misc.toSprite >> loadBlocks)

        drawer (fun x -> ("pressed", this, "ClickedItem", (SParam x))) this

    override this._Ready() =
        showMaker showBlocks showEnemies showMisc ToDisplay.Blocks
        this.DrawHand()

    override this._Input event =
        let position =
            match event with
            | :? InputEventMouseButton as button ->
                if button.ButtonIndex = (int) ButtonList.Left then
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
                match toShowState with
                | ToDisplay.Blocks ->
                    let insertBlockKind =
                        blockHand |> Hand.getSelected |> fun (_, v) -> v

                    let insertBlock = insertBlockKind |> BlockKinds.toBlock

                    let hasNeighbour =
                        drawnMap
                        |> Seq.filter (fun x -> x.Key <> roomCoordinateRounded)
                        |> Seq.filter (fun x -> IsNeighbour x.Key roomCoordinateRounded)
                        |> Seq.map (fun x -> (Direction.toDirection x.Key roomCoordinateRounded), x.Value)
                        |> Seq.exists (fun (dir, value) -> Block.fitBlocks value insertBlock dir)

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
                | ToDisplay.Enemies ->
                    let insertEnemy =
                        enemyHand |> Hand.getSelected |> fun (_, v) -> v

                    if drawnMap.ContainsKey roomCoordinateRounded then
                        let worldEnemyLocation = enemyMap.Value.WorldToMap mousePos

                        let tileEnemyLocation =
                            ((worldEnemyLocation.x
                              |> Mathf.Round
                              |> System.Convert.ToInt32) % 4,
                             (worldEnemyLocation.y
                              |> Mathf.Round
                              |> System.Convert.ToInt32) % 4)

                        let enemies = drawnMap.[roomCoordinateRounded].Enemies

                        if tileEnemyLocation |> enemies.ContainsKey |> not then
                            enemies.Add(tileEnemyLocation, insertEnemy)

                            let texture =
                                insertEnemy |> EnemyKinds.toPicture |> loadEnemies

                            let location =
                                enemyMap.Value.MapToWorld worldEnemyLocation

                            Some(texture, location, enemyMap.Value.CellSize)
                        else
                            None
                    else
                        None
                | ToDisplay.Misc -> None

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
                + (x.Enemies.Values |> Seq.sumBy EnemyKinds.toCost))

        this.UpdateCostBar z


    member this.OnBlocksPressed() =
        showMaker showBlocks showEnemies showMisc ToDisplay.Blocks
        this.DrawHand()

    member this.OnEnemiesPressed() =
        showMaker showEnemies showBlocks showMisc ToDisplay.Enemies
        this.DrawHand()

    member this.OnMiscPressed() =
        showMaker showMisc showBlocks showEnemies ToDisplay.Misc
        this.DrawHand()
