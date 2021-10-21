namespace PollClient

module PollingClient =

    open System.Collections.Generic
    open Godot
    open FSharp.Json

    let mutable private token: string option = None

    let mutable private url: string option = None
    let mutable private port: int option = None
    let mutable private usessl: bool option = None
    let clients = new List<HTTPClient>()

    let waitCheck (client: HTTPClient) =
        match client.GetStatus() with
        | HTTPClient.Status.Resolving
        | HTTPClient.Status.Requesting
        | HTTPClient.Status.Connecting ->
            match client.Poll() with
            | Error.Ok -> NotYet

            | x -> Got(Result.Error x)
        | status -> Got(Ok status)

    let fromGodotError x y =
        match y with
        | Error.Ok -> x ()
        | x ->
            GD.PrintErr(x)
            Result.Error(x)

    let private getConnection (client: HTTPClient) host port ssl =
        client.ConnectToHost(host, port, ssl, false)
        |> fromGodotError (fun () -> Ok(Poll(fun () -> waitCheck client)))
        |> Poll.FromResult
        |> Poll.Flatten
        |> Poll.MapOk(fun _ -> client)

    let private findConnection () =
        let found =
            clients
            |> Seq.tryFind (fun x -> x.GetStatus() = HTTPClient.Status.Connected)

        match found with
        | Some x -> x |> Ok |> Poll.Ready
        | None ->
            let newClient = new HTTPClient()

            (getConnection newClient url.Value port.Value usessl.Value)
            |> Poll.AfterOk(fun x -> x.GetStatus() |> ignore)

    let createUrl parts =
        parts |> List.fold (fun r s -> r + "/" + s) ""


    let connect host port1 ssl =
        url <- Some host
        port <- Some port1
        usessl <- Some ssl

        findConnection ()
        |> Poll.MapOk(fun x -> x.GetStatus())

    let bareRequest urlPart method (data: option<string>) =
        findConnection ()
        |> Poll.AndThenOk
            (fun client ->
                client.Request(
                    method,
                    urlPart,
                    [| if token.IsSome then
                           "authorization_token: " + token.Value
                       "Accept: application/json"
                       "Content-Type: application/json" |],
                    data |> Option.toObj
                )
                |> fromGodotError (fun () -> Ok(Poll(fun () -> waitCheck client)))
                |> Poll.FromResult
                |> Poll.Flatten
                |> Poll.AndThenOk
                    (fun _ ->
                        let mutable res: byte list = []

                        Poll
                            (fun () ->
                                match client.GetStatus() with
                                | HTTPClient.Status.Resolving
                                | HTTPClient.Status.Requesting
                                | HTTPClient.Status.Connecting -> NotYet
                                | HTTPClient.Status.Body ->
                                    res <-
                                        client.ReadResponseBodyChunk()
                                        |> Array.toList
                                        |> List.append res

                                    NotYet
                                | _ -> Got(Ok(res))))
                |> Poll.MapOk List.toArray)

    let requestSerialized<'T> urlPart method (data: option<string>) : Poll<Result<option<'T>, Error>> =
        (bareRequest urlPart method data)
        |> Poll.MapOk System.Text.Encoding.UTF8.GetString
        |> Poll.MapOk
            (fun x ->
                try
                    Some(Json.deserialize<'T> x)
                with
                | err ->
                    GD.PrintErr "Problem during deserialization. Error:"
                    GD.PrintErr err
                    GD.PrintErr "Got:"
                    GD.PrintErr x
                    None

                )

    let getBool urlPart =
        (bareRequest urlPart HTTPClient.Method.Get None)
        |> Poll.MapOk System.Text.Encoding.UTF8.GetString
        |> Poll.MapOk
            (fun x ->
                GD.Print("got back:")
                GD.Print(x)

                let trimmed = x.Trim().ToLower()

                if trimmed = "false" then Some false
                elif trimmed = "true" then Some true
                else None)


    let request<'T, 'A> urlPart method (data: Option<'A>) =
        data
        |> Option.map (Json.serialize)
        |> requestSerialized<'T> urlPart method

    let post<'T, 'A> (data: 'A) (urlPart: string) =
        data
        |> Some
        |> request<'T, 'A> urlPart HTTPClient.Method.Post

    let emptyPost<'T> urlPart =
        requestSerialized<'T> urlPart HTTPClient.Method.Post None

    let get<'T> urlPart =
        requestSerialized<'T> urlPart HTTPClient.Method.Get None
