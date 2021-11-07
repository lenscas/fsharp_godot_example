namespace super_dungeon_maker

open Godot
open GDUtils

type BonusState =
    | NotEnough
    | Succeeded
    | WithBonus

type BonusBarFs() as this =
    inherit Control()

    let minimunBar = this.getNode<ProgressBar> "./MinimunBar"

    let doneBar = this.getNode<ProgressBar> "./DoneBar"

    let bonusBar = this.getNode<ProgressBar> "./Bonus1Bar"

    let mutable currentValue = 0.

    let update () =
        let left =
            if minimunBar.Value.MaxValue <= currentValue then
                minimunBar.Value.Value <- minimunBar.Value.MaxValue
                currentValue - minimunBar.Value.MaxValue
            else
                minimunBar.Value.Value <- currentValue
                0.

        let left =
            if doneBar.Value.MaxValue <= left then
                doneBar.Value.Value <- doneBar.Value.MaxValue
                left - doneBar.Value.MaxValue
            else
                doneBar.Value.Value <- left
                0.

        if bonusBar.Value.MaxValue <= left then
            bonusBar.Value.Value <- doneBar.Value.MaxValue
        else
            bonusBar.Value.Value <- left

    member public _.AddTo v =
        currentValue <- currentValue + v
        update ()

    member public _.SetTo v =
        currentValue <- v
        update ()

    member public _.GetCurrentState() =

        if minimunBar.Value.MaxValue <= currentValue then
            let left = currentValue - minimunBar.Value.MaxValue

            if doneBar.Value.MaxValue <= left then
                let left = left - doneBar.Value.MaxValue

                if bonusBar.Value.MaxValue <= left then
                    WithBonus
                else
                    Succeeded
            else
                NotEnough
        else
            NotEnough
