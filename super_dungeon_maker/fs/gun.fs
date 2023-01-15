namespace super_dungeon_maker

open Godot
open SceneLoader

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
            let props = {
                Rotation = shooter.Rotation;
                Velocity = bulletConfig.Velocity;
                GlobalPosition = shooter.GlobalPosition;
                CollisionLayer = bulletConfig.CollisionLayer;
                CollisionMask = bulletConfig.CollisionMask;
                OnCollisionFunc = bulletConfig.Func;
            }
            let bulletScript = LoadScene<_,BulletFs> props
            parent.AddChild bulletScript
            timeSinceLastShot <- shootSpeed

    member public _._Process delta =
        timeSinceLastShot <- timeSinceLastShot - delta
