namespace Client

open PollClient
open PollClient.PollingClient

type GetByCode = { success: bool; code: string }
type CreateGroup = { success: bool; code: string }

module Login =

    let connectTo host ssl = connect host 8080 ssl

    let join (code: string) =
        [ "groups"; code ]
        |> createUrl
        |> emptyPost<GetByCode>

    let create () =
        [ "groups" ]
        |> createUrl
        |> emptyPost<CreateGroup>
        |> Poll.AndThenOk
            (fun x ->
                x
                |> Option.bind
                    (fun x ->
                        if x.success then
                            x.code
                            |> join
                            |> Poll.MapOk(fun y -> y |> Option.map (fun y -> x.code, y))
                            |> Some
                        else
                            None)
                |> Option.defaultWith (fun () -> None |> Ok |> Poll.Ready))
