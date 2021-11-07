namespace super_dungeon_maker

open Godot

module Rand =
    let private random =
        let a = new RandomNumberGenerator()
        a.Randomize()
        a

    let randInt a = random.RandiRange(0, a - 1)
    let randIntIncl a = (+) 1 >> randInt
    let randFloat = random.Randf

    let shuffle a =
        let b = a |> Seq.toArray
        let mutable n = b.Length

        while (n > 1) do
            n <- n - 1
            let k = random.RandiRange(0, n - 1)
            let value = b.[k]
            b.[k] <- b.[n]
            b.[n] <- value

        b

    let flip a b c = a c b

    let randomFrom s =
        let s = Seq.cache s
        s |> Seq.length |> randInt |> flip Seq.item s
