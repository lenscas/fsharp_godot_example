namespace super_dungeon_maker

open Godot
open GDUtils

type BasicEnemyFs() as this =
    inherit KinematicBody2D()

    let mutable target: Option<PlayerFs> = None
    let mutable navigator: Option<Navigation2D> = None
    let mutable isActivated = false

    let mutable walkTarget = Vector2()
    let mutable walkPath: Vector2 [] = [||]
    let mutable at = 0

    let mutable gun =
        Gun(
            5f,
            { Velocity = 10f
              Path = Defaults.bulletPath
              CollisionLayer = Defaults.noCollisionLayer
              CollisionMask = Defaults.playerLayer
              Func =
                  fun node ->
                      match node with
                      | :? PlayerFs as player -> player.GotHit()
                      | _ -> () }
        )


    let sprite =
        this.getNode<Node2D> "./EnemySpriteContainer"

    let rng = new RandomNumberGenerator()

    interface IEnemy with
        member this.GotHit() = this.QueueFree()


    [<Export>]
    member val ActivationRange = 500f with get, set

    [<Export>]
    member val Speed = 100f with get, set

    [<Export>]
    member val MinDistanceOf = 100f with get, set

    [<Export>]
    member val MaxDistanceOf = 500f with get, set

    //selecting a random value in a circle is hard.
    //selection sampling (generate a point, check if it is in the circle, start again if it isn't)
    //is probably the better one if you want a full circle, as it on avarage need less than 2 tries to get a correct point
    //however, we are working with a donut. This means that there are more invalid spaces, thus our amount of tries needed will increase
    //as a result, I opted to go with an algorithm that always generates a valid coordinate on the first try, and which can deal with donuts.
    //However, doing this the simple way of selecting a random radius and angle is biassed. This is because the further away from the center you go
    //the more valid points there are. This results in there being a bias towards the middle
    //So, we need to take this increase in points into acount while generating the numbers.
    //More information: https://www.youtube.com/watch?v=4y_nmpv-9lI
    member this.FindWalkTarget target (navigator: Navigation2D) =
        let theta = rng.Randf() * 2f * Mathf.Pi //select random angle

        let r = rng.Randf() |> Mathf.Sqrt //select the random distance

        let r =
            r * (this.MaxDistanceOf - this.MinDistanceOf)
            + this.MinDistanceOf //scale the distance to the size of the donut
        //set the target, used later to detect if the point is still valid
        walkTarget <-
            target
            + Vector2(r * Mathf.Cos(theta), r * sin (theta))

        walkPath <- navigator.GetSimplePath(this.Position, walkTarget) //create the path
        at <- 0 //go to point 0
        ()

    member public _.Configure newTarget newNavigator =
        target <- Some newTarget
        navigator <- Some newNavigator

    override _._Process x =
        gun._Process x
        () |> this.GetParent |> gun.Shoot sprite.Value

    override this._PhysicsProcess _ =
        match target with
        | Some target -> sprite.Value.LookAt target.Position
        | None -> ()

        match (isActivated, target, navigator) with
        | (false, Some target, Some navigator) ->
            if (target.Position |> this.Position.DistanceTo)
               <= this.ActivationRange then
                isActivated <- true
                this.FindWalkTarget target.Position navigator
        | (false, _, _)
        | (true, None, _)
        | (true, _, None) -> ()
        | (true, Some target, Some navigator) ->

            if walkPath.Length <= at
               || (walkTarget.DistanceTo target.Position) > this.MaxDistanceOf then
                this.FindWalkTarget target.Position navigator

            this.MoveAndSlide(Vector2(this.Speed, 0f).Rotated(this.Rotation))
            |> ignore

            this.LookAt(walkPath.[at])


            if this.GlobalTransform.origin.DistanceTo(walkPath.[at]) < 1f then
                at <- at + 1
