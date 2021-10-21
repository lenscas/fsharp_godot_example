namespace super_dungeon_maker

open Godot
open GDUtils

type DungeonFs() as this =
    inherit Node2D()

    let roomSize = 64
    let openingSize = 14
    let enemySpots = 4

    let wall = 0
    let floor = 1


    let camera = this.getNode<PlayerFs> "./Player"

    let tileMap =
        this.getNode<TileMap> "./Navigator/DungeonMap"

    let navigator = this.getNode<Navigation2D> "./Navigator"

    let hpBar =
        this.getNode<ProgressBar> "./GuiLayer/HealthBar"

    member public this.StartDungeon(dungeon: System.Collections.Generic.Dictionary<(int * int), Block>) =
        let map = tileMap.Value
        map.Clear()
        hpBar.Value.Visible <- true
        for ((x, y), block) in dungeon |> Seq.map (fun x -> x.Key, x.Value) do

            let (expandedX, expandedY) = (x * roomSize, y * roomSize)

            camera.Value.GlobalPosition <-
                Vector2((float32 expandedX) + (float32 roomSize) / 2f, (float32 expandedY) + (float32 roomSize) / 2f)
                |> map.MapToWorld

            let toCheck =
                seq { roomSize / 2 - openingSize / 2 .. roomSize / 2 + openingSize / 2 }
                |> Seq.toList

            let hasNeighbourLeft = dungeon.ContainsKey(x - 1, y)
            let hasNeighbourRight = dungeon.ContainsKey(x + 1, y)
            let hasNeighbourUp = dungeon.ContainsKey(x, y - 1)
            let hasNeighbourDown = dungeon.ContainsKey(x, y + 1)

            let checker coordinate minus hasNeighbour kind =
                not (
                    hasNeighbour
                    && toCheck |> Seq.contains (coordinate - minus)
                    && block.Openings |> Seq.contains kind
                )


            let maxX = expandedX + roomSize
            let maxY = expandedY + roomSize

            for tileX in seq { expandedX .. maxX } do
                let checkerX = checker tileX expandedX

                for tileY in seq { expandedY .. maxY } do
                    let checkerY = checker tileY expandedY

                    let toDraw =
                        if (tileY = expandedY && checkerX hasNeighbourUp Up)
                           || (tileY = maxY && checkerX hasNeighbourDown Down)
                           || (tileX = expandedX
                               && checkerY hasNeighbourLeft Left)
                           || (tileX = maxX && checkerY hasNeighbourRight Right) then
                            wall
                        else
                            floor

                    map.SetCell(tileX, tileY, toDraw)

            for ((enemyX, enemyY), enemy) in block.Enemies |> Seq.map (fun x -> x.Key, x.Value) do
                let (expandedEnemyX, expandedEnemyY) =
                    expandedX + (enemyX * (roomSize / enemySpots)), expandedY + (enemyY * (roomSize / enemySpots))

                let enemyScene =
                    enemy |> EnemyKinds.toNode |> loadEnemyNode

                let enemyNode = enemyScene.Instance() :?> BasicEnemyFs

                enemyNode.GlobalPosition <-
                    Vector2(expandedEnemyX |> float32, expandedEnemyY |> float32)
                    |> map.MapToWorld

                enemyNode.Configure camera.Value navigator.Value

                this.AddChild(enemyNode)
                ()

        camera.Value.EnableCam(fun x -> hpBar.Value.Value <- x)
        ()
