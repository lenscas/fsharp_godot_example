namespace super_dungeon_maker

type Misc =
    | Start
    | End

module Misc =
    let toPicture =
        function
        | Start -> "start"
        | End -> "end"

    let toCost =
        function
        | Start
        | End -> 0

    let toNode =
        function
        | Start -> "Start"
        | End -> "End"

type Direction =
    | Up
    | Down
    | Left
    | Right

module Direction =
    let oposite =
        function
        | Up -> Down
        | Down -> Up
        | Left -> Right
        | Right -> Left

    let toDirection (firstX, firstY) (secondX, secondY) =
        if firstX - 1 = secondX then Left
        else if firstX + 1 = secondX then Right
        else if firstY - 1 = secondY then Up
        else Down


type EnemyKinds =
    | IAMBROKEN = 0
    | BasicEnemy = 20

module EnemyKinds =
    let private handleBadEnumValue<'a> (x: 'a) =
        Godot.GD.PrintErr("Got an invalid value for `enemy`. Got:\n")
        Godot.GD.PrintErr(x)

        (x.ToString() + "is not a valid enemyKind", "_arg1")
        |> System.ArgumentException

    let toPicture =
        function
        | EnemyKinds.BasicEnemy -> "enemy"
        | x -> x |> handleBadEnumValue |> raise

    let toNode =
        function
        | EnemyKinds.BasicEnemy -> "BasicEnemy"
        | x -> x |> handleBadEnumValue |> raise

    let toCost =
        function
        | EnemyKinds.BasicEnemy -> 20
        | x -> x |> handleBadEnumValue |> raise


type Items =
    | Enemy of EnemyKinds
    | Misc of Misc

type Block =
    { Openings: List<Direction>
      Items: System.Collections.Generic.Dictionary<int * int, Items> }

module Block =

    let fitBlocks blockA blockB how =
        let openingA = (blockA.Openings |> List.contains how)
        Godot.GD.Print how
        Godot.GD.Print openingA

        let openingB =
            blockB.Openings
            |> List.contains (Direction.oposite how)

        Godot.GD.Print openingB
        openingA && openingB

type BlockKinds =
    | AllOpen = 0
    | UpAndDown = 1
    | LeftAndRight = 2


module BlockKinds =
    let private handleBadEnumValue<'a> (x: 'a) =
        Godot.GD.PrintErr("Got an invalid value for `toBlock`. Got:\n")
        Godot.GD.PrintErr(x)

        (x.ToString() + "is not a valid BlockKind", "_arg1")
        |> System.ArgumentException

    let toBlock _arg1 =
        let openings =
            match _arg1 with
            | BlockKinds.AllOpen -> [ Up; Down; Left; Right ]
            | BlockKinds.UpAndDown -> [ Up; Down ]
            | BlockKinds.LeftAndRight -> [ Left; Right ]
            | x -> x |> handleBadEnumValue |> raise

        { Openings = openings
          Items = System.Collections.Generic.Dictionary() }

    let toPicture =
        function
        | BlockKinds.AllOpen -> "AllOpen"
        | BlockKinds.UpAndDown -> "UpAndDown"
        | BlockKinds.LeftAndRight -> "LeftAndRight"
        | x -> x |> handleBadEnumValue |> raise
