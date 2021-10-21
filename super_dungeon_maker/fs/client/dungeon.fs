namespace Client

open PollClient.PollingClient

type SubmittedDungeon = { success: bool }



module Dungeon =
    let SubmitDungeon groupCode playerCode dungeon =
        [ "groups"
          groupCode
          "players"
          playerCode ]
        |> createUrl
        |> post<SubmittedDungeon, _> dungeon

    let IsEveryoneDone groupCode playerCode =
        [ "groups"
          groupCode
          "is_everyone_done" ]
        |> createUrl
        |> getBool
