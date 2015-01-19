module Program

open System
open Futures

[<EntryPoint>]
let main argv = 

    Console.Write ("GitHub username : ")
    let userName = Console.ReadLine ()

    printfn ""
    printfn "Getting data:"

    let future = Example.getData userName
    let outcome = Future.run future

    printfn ""
    printfn "Outcome:"

    match outcome with
    | Success (name, url, repos) ->     

        printfn "  Success:"
        printfn "    Name: %s" name
        printfn "    URL: %s" url
        printfn "    Repositories: %u" repos

    | Failure e -> 

        printfn "  Failure:"
        printfn "    %s" e.Message

    Console.ReadLine ()
    |> ignore

    let add n value = Future.wrap (async { return (n + value) })

    let addTwoThenThree x = 
        add 2 x
        |> Future.flatMap (add 3)

    0 // return an integer exit code
