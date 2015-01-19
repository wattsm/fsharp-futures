[<RequireQualifiedAccess>]
module Example

open System
open System.IO
open System.Net
open Futures
open Futures.Operators
open Newtonsoft.Json.Linq

let private _selectValue path (json : JObject) = 
    match (json.SelectToken path) with        
    | :? JValue as value -> Some value.Value
    | _ -> None
        
let createRequest userName = 
    Future.wrap (async {
        printfn "  Preparing HTTP request"

        let url = sprintf "https://api.github.com/users/%s" userName
        let request = HttpWebRequest.Create (url) :?> HttpWebRequest

        return request
    })

let configureRequest (request : HttpWebRequest) = 
    Future.wrap (async {

        printfn "  Authenticating HTTP request"

        request.Method <- "GET"
        request.Accept <- "application/vnd.github.v3+json"
        request.UserAgent <- "FSharp-Futures-Example"

        return request
    })

let sendRequest (request : HttpWebRequest) = 
    Future.wrap (async {

        printfn "  Sending HTTP request"

        let! response = Async.AwaitTask (request.GetResponseAsync ())

        return (response :?> HttpWebResponse)
    })

let handleException (ex : Exception) = 

    let handleWebException (webEx : WebException) = 
        if (webEx.Response <> null) then
            let response = (webEx.Response :?> HttpWebResponse)
            in (Success response)
        else
            Failure (webEx :> Exception)

    async {

        printfn "  Handling exception"

        match ex with
        | :? WebException as webEx -> return (handleWebException webEx)
        | :? AggregateException as aggEx ->
            match aggEx.InnerException with
            | :? WebException as webEx -> return (handleWebException webEx)
            | _ -> return (Failure ex)
        | _ -> return (Failure ex)
    }

let verifyStatus (response : HttpWebResponse) = 
    async {

        printfn "  Verifying HTTP status code"

        match response.StatusCode with
        | HttpStatusCode.OK -> return (Success response)
        | _ -> 
            let message = sprintf "HTTP request failed with status: %O" response.StatusCode
            let e = InvalidOperationException (message)

            return (Failure e)
    }

let parseJson (response : HttpWebResponse) = 
    Future.wrap (async {

        printfn "  Parsing JSON response"

        use stream = response.GetResponseStream ()
        use reader = new StreamReader (stream)

        let! raw = Async.AwaitTask (reader.ReadToEndAsync ())
        let json = JObject.Parse (raw)

        return json
    })

let summarise (json : JObject) = 

    printfn "  Summarising data"

    let name = 
        match (_selectValue ("$['name']") json) with
        | Some value -> (string value)
        | _ -> String.Empty

    let url = 
        match (_selectValue "$['blog']" json) with
        | Some value -> (string value)
        | _ -> String.Empty

    let repos = 
        match (_selectValue "$['public_repos']" json) with
        | Some value -> (Convert.ToInt32 value)
        | _ -> 0

    (name, url, repos)

let getData = 
    createRequest
    ->> configureRequest
    ->> sendRequest
    --| handleException
    ->> verifyStatus
    ->> parseJson
    --> summarise