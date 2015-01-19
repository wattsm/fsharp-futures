module Futures

open System

///Represents the outcome of a computation
type Outcome<'a> = 
    | Success of 'a
    | Failure of Exception

///Functions for working with the Outcome<'a> type
[<RequireQualifiedAccess>]
module Outcome =     

    ///Convert a choice to an outcome
    let ofChoice = function
        | Choice1Of2 value -> Success value
        | Choice2Of2 e -> Failure e

///Type representing a future / promise
type Future<'a> = Async<Outcome<'a>>

///Functions for working with the Future<'a> type
[<RequireQualifiedAccess>]
module Future = 

    ///Create a future from an async computation
    let wrap computation = 
        async {

            let! choice = (Async.Catch computation)
        
            return (Outcome.ofChoice choice)
        }

    ///Create a future from a value
    let value x = 
        wrap (async { return x })

    ///Evaluate a future
    let run = Async.RunSynchronously

    ///Map the success outcome of a future
    let flatMap f future = 
        async {

            let! outcome = future
            
            match outcome with
            | Success value -> return! (f value)
            | Failure e -> return (Failure e)
        }

    ///Map the success outcome of a future
    let map f = 
        let f' value = wrap (async { return (f value) })
        in flatMap f'

    ///Rescue a failed future
    let rescue f future = 
        async {
        
            let! outcome = future

            match outcome with
            | Success value -> return (Success value)
            | Failure e -> return! (f e)
        }

    ///Bind two futures returning functions together
    let bind f g = f >> (flatMap g)

    ///Chain a list of futures returning functions together
    let chain futures = 
        futures
        |> List.fold bind value 

///Infix operators for working with futures
module Operators = 

    ///Create a future by binding two together
    let (->>) = Future.bind

    ///Create a future by mapping one
    let (-->) f g = f >> (Future.map g)

    ///Combine a future and a rescue function
    let (--|) f g = f >> (Future.rescue g)