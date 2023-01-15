namespace ErrorHandling

//the below function is taken from https://github.com/demystifyfp/FsToolkit.ErrorHandling/blob/master/src/FsToolkit.ErrorHandling/Seq.fs#L6

[<RequireQualifiedAccess>]
module Seq =

    let sequenceResultM (xs: seq<Result<'t, 'e>>) : Result<'t seq, 'e> =
        let rec loop xs ts =
            match Seq.tryHead xs with
            | Some x -> x |> Result.bind (fun t -> loop (Seq.tail xs) (t :: ts))
            | None -> Ok(List.rev ts |> List.toSeq)

        // Seq.cache prevents double evaluation in Seq.tail
        loop (Seq.cache xs) []
