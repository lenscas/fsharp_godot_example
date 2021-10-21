namespace super_dungeon_maker

open Godot

type BulletConfig =
    { Func: Node -> unit
      Velocity: float32
      Path: string
      CollisionMask: uint32
      CollisionLayer: uint32 }

type Gun(shootSpeed, bulletConfig) =

    let mutable timeSinceLastShot = 0f

    member public _.Shoot (shooter: Node2D) (parent: Node) =

        if timeSinceLastShot < 0f then
            GD.Print "SHOOT TIME!"
            let bullet = GD.Load<PackedScene>(bulletConfig.Path)
            let bulletScript = bullet.Instance() :?> BulletFs
            bulletScript.GlobalRotation <- shooter.GlobalRotation
            bulletScript.Velocity <- bulletConfig.Velocity
            bulletScript.GlobalPosition <- shooter.GlobalPosition
            bulletScript.CollisionLayer <- bulletConfig.CollisionLayer
            bulletScript.CollisionMask <- bulletConfig.CollisionMask
            bulletScript.OnCollisionFunc <- bulletConfig.Func
            parent.AddChild bulletScript
            timeSinceLastShot <- shootSpeed

    member public _._Process delta =
        timeSinceLastShot <- timeSinceLastShot - delta
