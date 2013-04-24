module Validation

let validator pred error x =
    if pred x then Choice1Of2 x
    else Choice2Of2 error

let (==) = LanguagePrimitives.PhysicalEquality
let inline (!=) a b = not (a == b)
let notNull e = validator ((!=) null) e
let notEqual a = validator ((<>) a)
let notEmptyString e = validator (fun (s:string) -> s != null && s.Length > 0) e

/// Given a value, creates a choice 1.
let puree = Choice1Of2

/// Given a function in a choice and a choice value x, applies the function to the value if available, 
/// otherwise propagates the second choice.
let apply f x =
    match f,x with
    | Choice1Of2 f, Choice1Of2 x   -> Choice1Of2 (f x)
    | Choice2Of2 e, Choice1Of2 x   -> Choice2Of2 e
    | Choice1Of2 f, Choice2Of2 e   -> Choice2Of2 e
    | Choice2Of2 e1, Choice2Of2 e2 -> Choice2Of2 (e1 @ e2)

let (<*>) = apply

/// Applies the function to the choice 1 value and returns the result as a choice 1, if matched, 
/// otherwise returns the original choice 2 value.
let map f o =
    match o with
    | Choice1Of2 x -> f x |> puree
    | Choice2Of2 x -> Choice2Of2 x

let inline (<!>) f x = map f x
let inline lift2 f a b = f <!> a <*> b

let inline ( *>) a b = lift2 (fun _ z -> z) a b
let inline ( <*) a b = lift2 (fun z _ -> z) a b

let (>>=) m f =
    match m with
    | Choice1Of2 x -> f x
    | Choice2Of2 x -> Choice2Of2 x

let (>>.) m1 m2 = m1 >>= (fun _ -> m2)

let inline flip f a b = f b a
let inline cons a b = a::b

let seqValidator f = 
    let zero = puree []
    Seq.map f >> Seq.fold (lift2 (flip cons)) zero    