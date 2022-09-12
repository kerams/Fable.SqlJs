# Fable.SqlJs

Fable bindings for the SQLite library in JS [sql.js](https://github.com/sql-js/sql.js) ([NPM package](https://www.npmjs.com/package/sql.js)) version ^1.8.0. Web worker bits are not covered.

## Nuget package
[![Nuget](https://img.shields.io/nuget/v/Fable.SqlJs.svg?colorB=green)](https://www.nuget.org/packages/Fable.SqlJs)

## Installation with [Femto](https://github.com/Zaid-Ajaj/Femto)

```
femto install Fable.SqlJs
```

## Standard installation

Nuget package

```
paket add Fable.SqlJs -p YourProject.fsproj
```

NPM package

```
npm install sql.js@1.8.0
```

## Usage

The sql.js library consists of a JS part and a WebAssembly part. Both need to be distributed with your application. If you're using Webpack 5, calling `initSqlJsWebpack5` should do most of the work. Otherwise you will use `initSqlJs` and point the library to the `sql-wasm.wasm` file.

```fsharp
open Fable.SqlJs
open Fable.Core.JsInterop
open Fable.Core

// Type annotation for `x` is required, otherwise we would get wrong type matches.
let valueToString (x: SqlValue option) =
    match x with
    | None -> "null" // null values
    | Some (U3.Case1 x) -> x.ToString () // numbers
    | Some (U3.Case3 x) -> $"[{x}]" // binary data
    | Some (U3.Case2 x) -> x // strings

// Load the WASM file locally in Webpack 5. It will be automatically copied by Webpack as an asset.
// You might also have to add the following to module.exports in the Webpack config file:
//
//    resolve: {
//        fallback: {
//            "crypto": false,
//            "fs": false,
//            "path": false
//       }
//    },
let sqlJs = initSqlJsWebpack5 ()

// Load the WASM file from /sql-wasm.wasm. You will need to make sure to deploy this file to the web server.
//let db = initSqlJs (jsOptions<SqlJsConfig>(fun x -> x.locateFile <- fun f -> f))
// or
//let db = initSqlJs JS.undefined

// Load the WASM file from a CDN. Make sure to reference the matching version.
//let db = initSqlJs (jsOptions<SqlJsConfig>(fun x -> x.locateFile <- fun f -> $"https://cdnjs.cloudflare.com/ajax/libs/sql.js/1.8.0/{f}"))

// Do stuff once the initialization promise resolves. In an Elmish application you will typically want to pass `sqlJs` to `Cmd.OfPromise.either`.
sqlJs.``then``(fun x ->
    // Create an empty database. To open an existing one, pass in the byte array as the parameter instead.
    let db = x.CreateDatabase ()

    // Execute a command, discarding any return values.
    db.run "CREATE TABLE thing(id integer, name text, picture binary)"

    // Execute a command, discarding any return values.
    // Parameters are passed in in an array and are applied positionally.
    // !^ applies erased casts to make parameter passing a bit easier since the JS API is very dynamic.
    db.run ("INSERT INTO thing VALUES (1, $name1, null), ($id2, 'car', null), (3, $name3, $picture3)", !^[| !^"ball"; !^2.; !!null; !^[| 222uy; 223uy |] |])
    
    // Execute 2 commands, which will gives us an array with 2 elements with the rows returned by each command.
    // This time we have decided to pass the parameters in as a JS object, which we are constructing using an anonymous record.
    // Now the parameters are matched by name, not positionally.
    let resultSets = db.exec ("SELECT sqlite_version(); SELECT id, picture, name FROM thing WHERE id > $id", !^{| ``$id`` = 1 |})

    // Print the DB version using the first column in the first row of the first result set.
    printfn $"SQLite version: {resultSets.[0].values.[0].[0]}"

    // Iterate over the second result set, printing the values in each row.
    // The order of values in the `columns` and `values` matches, and is the same as the order in the SELECT clause.
    for columnValuesInRow in resultSets.[1].values do
        for i, column in Array.indexed resultSets.[1].columns do
            printfn $"{column}: {valueToString columnValuesInRow.[i]}"

        printfn ""

    // Export (back up) the database as a byte array to store it somewhere.
    let dbData = db.export ()

    // Close the database when done to free the resources.
    db.close ()
) |> ignore
```
