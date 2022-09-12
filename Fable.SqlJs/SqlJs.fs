module Fable.SqlJs

open Fable.Core
open Fable.Core.JsInterop

type SqlJsConfig =
    /// A function that returns the full path to a resource given its file name
    abstract locateFile: (string -> string) with get, set

type SqlValue = U3<float, string, byte[]>

type BindParams = U2<SqlValue[], obj>

type QueryExecResult =
    /// The name of the columns of the result (as returned by Statement.getColumnNames)
    abstract columns: string[] 

    /// One array per row, containing the column values
    abstract values: (SqlValue option)[][]

type Statement =
    /// Bind values to the parameters, after having reseted the statement. If values is null, do nothing and return true.
    /// SQL statements can have parameters, named '?', '?NNN', ':VVV', '@VVV', '$VVV', where NNN is a number and VVV a string. This function binds these parameters to the given values.
    ///
    /// Warning: ':', '@', and '$' are included in the parameters names
    abstract bind: values: BindParams -> bool

    /// Free the memory used by the statement
    abstract free: unit -> bool

    /// Free the memory allocated during parameter binding
    abstract freemem: unit -> unit

    /// Get one row of results of a statement. If the first parameter is not provided, step must have been called before.
    abstract get: ?``params``: BindParams -> (SqlValue option)[]

    /// Get one row of result as a javascript object, associating column names with their value in the current row.
    abstract getAsObject: ?``params``: BindParams -> obj

    /// Get the list of column names of a row of result of a statement.
    abstract getColumnNames: unit -> string[]

    /// Get the SQLite's normalized version of the SQL string used in preparing this statement. The meaning of "normalized" is not well-defined: see the SQLite documentation.
    abstract getNormalizedSQL: unit -> string

    /// Get the SQL string used in preparing this statement.
    abstract getSQL: unit -> string

    /// Reset a statement, so that it's parameters can be bound to new values It also clears all previous bindings, freeing the memory used by bound parameters.
    abstract reset: unit -> unit

    /// Shorthand for bind + step + reset Bind the values, execute the statement, ignoring the rows it returns, and resets it
    abstract run: ?values: BindParams -> unit

    /// Execute the statement, fetching the the next line of result, that can be retrieved with Statement.get.
    abstract step: unit -> bool

type Database =
    /// Close the database, and all associated prepared statements. The memory associated to the database and all associated statements will be freed.
    ///
    /// Warning: A statement belonging to a database that has been closed cannot be used anymore.
    /// Databases must be closed when you're finished with them, or the memory consumption will grow forever
    abstract close: unit -> unit

    /// Execute an sql statement, and call a callback for each row of result.
    abstract each: sql: string * ``params``: BindParams * callback: (obj -> unit) * ``done``: (unit -> unit) -> Database

    /// Execute an sql statement, and call a callback for each row of result.
    abstract each: sql: string * callback: (QueryExecResult -> unit) * ``done``: (unit -> unit) -> Database

    /// Execute an SQL query, and returns the result.
    ///
    /// This is a wrapper against Database.prepare, Statement.bind, Statement.step, Statement.get, and Statement.free.
    /// The result is an array of result elements. There are as many result elements as the number of statements in your sql string (statements are separated by a semicolon)
    abstract exec: sql: string * ?``params``: BindParams -> QueryExecResult[]

    /// Execute an SQL query, ignoring the rows it returns.
    abstract run: sql: string * ?``params``: BindParams -> unit

    /// Returns the number of changed rows (modified, inserted or deleted) by the latest completed INSERT, UPDATE or DELETE statement on the database. Executing any other type of SQL statement does not modify the value returned by this function.
    abstract getRowsModified: unit -> int

    /// Exports the contents of the database to a binary array
    abstract export: unit -> byte[]

    /// Prepare an SQL statement
    abstract prepare: sql: string * ?``params``: BindParams -> Statement

    /// Iterate over multiple SQL statements in a SQL string. This function returns an iterator over Statement objects. You can use a for..of loop to execute the returned statements one by one.
    abstract iterateStatements: sql: string -> seq<Statement>

    /// Register a custom aggregate with SQLite
    abstract create_aggregate: name: string * aggregateFunctions: obj -> Database

    /// Register a custom function with SQLite
    abstract create_function: name: string * func: (obj -> obj) -> Database

    /// Analyze a result code, return null if no error occured, and throw an error with a descriptive message otherwise
    abstract handleError: unit -> obj option

type SqlJs =
    [<Emit("new $0.Database($1)")>]
    abstract CreateDatabase: ?data: byte[] -> Database

    /// A class that represents an SQLite database
    abstract Database: Database

    /// The prepared statement class
    abstract Statement: Statement

/// Asynchronously initializes sql.js
let initSqlJs: SqlJsConfig -> JS.Promise<SqlJs> = importDefault "sql.js"

/// Asynchronously initializes sql.js
/// Automatically resolves the WASM file when used in Webpack 5 and loads it as an asset
let initSqlJsWebpack5 () = initSqlJs (jsOptions<SqlJsConfig>(fun x -> x.locateFile <- fun _ -> emitJsExpr () "new URL('sql.js/dist/sql-wasm.wasm', import.meta.url).toString()"))
