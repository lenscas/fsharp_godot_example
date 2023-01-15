namespace super_dungeon_maker

open Godot
open GDUtils

type DungeonProps = {
    dungeon: System.Collections.Generic.Dictionary<(int * int), Block>;
    after: unit -> unit
}

type DungeonFs() =
    inherit DungeonScene<DungeonProps>()

    let roomSize = 64
    let openingSize = 14
    let itemSpots = 4
    let endLocation = (3, 3)
    let wall = 0
    let floor = 1
    let mutable dungeon = System.Collections.Generic.Dictionary()
    let mutable after = ignore
 
    override this.Setup(a) = 
        GD.Print "in setup"
        dungeon <- a.dungeon
        after <- a.after

    member public this.StartDungeon () =
        GD.Print "in start dungeon"
        let map = this.Navigator.DungeonMap.UnwrappedNode
        map.Clear()
        this.GuiLayer.HealthBar.UnwrappedNode.Visible <- true

        let endBlock =
            dungeon.Values
            |> Seq.filter (fun x -> endLocation |> x.Items.ContainsKey |> not)
            |> Seq.filter (fun x ->
                Misc.Start
                |> Items.Misc
                |> x.Items.ContainsValue
                |> not)
            |> Rand.randomFrom

        endBlock.Items.Add(endLocation, Misc.End |> Items.Misc)

        for ((x, y), block) in dungeon |> Seq.map (fun x -> x.Key, x.Value) do

            let (expandedX, expandedY) = (x * roomSize, y * roomSize)

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

            let z = roomSize / itemSpots

            for ((itemX, itemY), item) in block.Items |> Seq.map (fun x -> x.Key, x.Value) do

                let (expandedItemX, expandedItemY) =
                    expandedX + (itemX * z) + (z / 2), expandedY + (itemY * z) + (z / 2)

                match item with
                | Items.Enemy enemy ->
                    let position = 
                        Vector2(expandedItemX |> float32, expandedItemY |> float32)
                        |> map.MapToWorld
                    let props = {target=this.Player.Value; navigator=this.Navigator.UnwrappedNode;GlobalPosition=position}

                    let enemyScene =
                        enemy |> EnemyKinds.toNode |> loadEnemyNode
                    
                    let enemyNode = enemyScene.Instance() :?> BasicEnemyFs
                    
                    enemyNode.Setup props
                    this.AddChild enemyNode
                | Items.Misc x ->
                    let miscScene = x |> Misc.toNode |> loadMiscNode
                    let miscNode = miscScene.Instance() :?> Node2D

                    miscNode.GlobalPosition <-
                        Vector2(expandedItemX |> float32, expandedItemY |> float32)
                        |> map.MapToWorld

                    match x with
                    | Start ->
                        this.Player.Value.GlobalPosition <-
                            Vector2(
                                (float32 expandedX) + (float32 roomSize) / 2f,
                                (float32 expandedY) + (float32 roomSize) / 2f
                            )
                            |> map.MapToWorld
                    | End ->
                        let endNode = miscNode :?> EndFs
                        endNode.RunOnReached <- after

                    this.AddChild miscNode

        this.Player.Value.EnableCam(fun x -> this.GuiLayer.HealthBar.UnwrappedNode.Value <- x)
        ()
    override this._Ready () = this.StartDungeon ()
