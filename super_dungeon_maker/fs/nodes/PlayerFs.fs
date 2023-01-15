namespace super_dungeon_maker

open Godot
open GDUtils
open SceneLoader

type PlayerState =
    | Ready
    | WaitUntilFire of float32

type PlayerFs() as this =
    inherit PlayerScene()


    let mutable currentHp = 100.

    let mutable state = Ready

    member public this.EnableCam func =
        this.PlayerCam.UnwrappedNode.Current <- true
        this.UpdateHPBar <- func

    [<Export>]
    member val BulletPath = "res://prefabs/bullet.tscn" with get, set

    member val UpdateHPBar: float -> unit = ignore with get, set

    [<Export>]
    member val CooldownTime = 2f with get, set


    [<Export>]
    member val Speed = 100f with get, set

    member public _.GotHit() =
        currentHp <- currentHp - 10.
        this.UpdateHPBar currentHp


    member this.GetInput() =
        let mutable velocity = Vector2()

        if Input.IsActionPressed("ui_right") then
            velocity.x <- velocity.x + 1f

        if Input.IsActionPressed("ui_left") then
            velocity.x <- velocity.x - 1f

        if Input.IsActionPressed("ui_down") then
            velocity.y <- velocity.y + 1f

        if Input.IsActionPressed("ui_up") then
            velocity.y <- velocity.y - 1f

        velocity.Normalized() * this.Speed

    override this._Process delta =
        state <-
            match state with
            | Ready -> Ready
            | WaitUntilFire x ->
                let newTime = x - delta

                if newTime < 0f then
                    Ready
                else
                    WaitUntilFire newTime

        ()
        |> this.GetGlobalMousePosition
        |> this.PlayerSpriteContainer.UnwrappedNode.LookAt

    override this._Input input =
        match (state, input) with
        | (Ready, (:? InputEventMouseButton as button)) ->
            if button.ButtonIndex = (int) ButtonList.Left then
                let collisionFunc = 
                    fun x ->
                        match box x with
                        | :? IEnemy as enemy -> enemy.GotHit()
                        | x -> GD.Print x
                
                let props = {
                    Rotation = this.PlayerSpriteContainer.UnwrappedNode.Rotation;
                    Velocity = 10f;
                    GlobalPosition = this.GlobalPosition;
                    CollisionLayer = 0u;
                    CollisionMask = 2u;
                    OnCollisionFunc = collisionFunc
                }
                let bulletScript = LoadScene<_,BulletFs> props
                this.GetParent().AddChild bulletScript
                state <- WaitUntilFire this.CooldownTime
        | (WaitUntilFire x, (:? InputEventMouseButton as button)) ->
            if button.ButtonIndex = (int) ButtonList.Left then
                GD.Print "Wanted to fire but still need to wait"
                GD.Print x
        | (_, _) -> ()


    override this._PhysicsProcess _ =
        () |> this.GetInput |> this.MoveAndSlide |> ignore
//this.Position <- Vector2(pos.x + velocity.x, pos.y + velocity.y)
