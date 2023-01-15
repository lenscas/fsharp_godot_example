namespace super_dungeon_maker

open Godot


type BulletProps = {
    Rotation : float32;
    Velocity : float32;
    GlobalPosition : Vector2;
    CollisionLayer : uint32;
    CollisionMask : uint32;
    OnCollisionFunc: Node -> unit;
}

type BulletFs() =
    inherit BulletScene<BulletProps>()

    member val Velocity = 0f with get, set
    member val OnCollisionFunc = ignore with get, set
    override this.Setup a = 
        this.Rotation <- a.Rotation
        this.Velocity <- a.Velocity
        this.GlobalPosition <- a.GlobalPosition
        this.CollisionLayer <- a.CollisionLayer
        this.CollisionMask <- a.CollisionMask
        this.OnCollisionFunc <- a.OnCollisionFunc
    override this._PhysicsProcess _ =
        this.Position <-
            this.Position
            + Vector2(this.Velocity, 0f).Rotated(this.Rotation)


        ()

    member this.OnCollision(body: Node) =
        this.OnCollisionFunc body
        this.QueueFree()
