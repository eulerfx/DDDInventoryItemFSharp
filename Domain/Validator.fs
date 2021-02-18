module Validator

let validator predicate error x =
    if predicate x then Choice1Of2 x
    else Choice2Of2 error

let (==) = LanguagePrimitives.PhysicalEquality
let inline (!=) a b = not (a == b)
let notNull e = validator ((!=) null) e
let notEmptyString e = validator (fun (s:string) -> s.Length > 0) e

/// Given a value, creates a choice 1. (Applicative functor)
let puree = Choice1Of2

/// Given a function in a choice and a choice value x, applies the function to the value if available, 
/// otherwise propagates the second choice. (Applicative functor)
let apply f x =
    match f,x with
    | Choice1Of2 f, Choice1Of2 x   -> Choice1Of2 (f x)
    | Choice2Of2 e, Choice1Of2 x   -> Choice2Of2 e
    | Choice1Of2 f, Choice2Of2 e   -> Choice2Of2 e
    | Choice2Of2 e1, Choice2Of2 e2 -> Choice2Of2 (e1 @ e2)

let (<*>) = apply

/// Applies the function to the choice 1 value and returns the result as a choice 1, if matched, 
/// otherwise returns the original choice 2 value. (Functor)
let map f o =
    match o with
    | Choice1Of2 x -> f x |> puree
    | Choice2Of2 x -> Choice2Of2 x

let inline (<!>) f x = map f x

/// Lifts a two argument function to operate over the choice type.
let inline lift2 f a b = f <!> a <*> b

/// Composes two choice types, passing the case-1 type of the right value.
let inline ( *>) a b = lift2 (fun _ z -> z) a b

/// Composes two choice types, passing the case-2 type of the left value.
let inline ( <*) a b = lift2 (fun z _ -> z) a b

/// Composes a choice type with a non choice type.
let inline (<?>) a b = lift2 (fun _ z -> z) a              (Choice1Of2 b)

/// Composes a non-choice type with a choice type.
let inline (|?>) a b = lift2 (fun z _ -> z) (Choice1Of2 a) b