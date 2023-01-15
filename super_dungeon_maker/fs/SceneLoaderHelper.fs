namespace super_dungeon_maker
open Godot
type ISceneLoader<'t> = 
    abstract Setup: 't -> unit

module SceneLoader = 
    let inline LoadScene<'SceneParams, ^t when ^t : (static member GetScenePath: unit -> string) and ^t :> ISceneLoader<'SceneParams> and ^t :> Node > (sceneParams:'SceneParams) =
        let resPath = (^t : (static member GetScenePath: unit -> string) ())
        let scene = GD.Load<PackedScene> resPath
        let node = scene.Instance() :?> ^t
        node.Setup sceneParams
        node
