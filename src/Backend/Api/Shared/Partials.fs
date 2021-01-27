namespace Backend.Api.Shared

open Giraffe.ViewEngine
open Backend

        
module Partials =

    let submitButton (text: string) =
        div [ 
            _class "field" 
            _style "margin-top: 10px;"
        ] [ 
            div [ _class "control" ] [
                button [ _class "button is-primary"; _type "submit" ] [
                    str text
                ]
            ]
        ]

    let submitButtonWithCancel (okText: string) (cancelText: string) cancelHref =
        div [ 
            _class "field is-grouped" 
            _style "margin-top: 10px;"
        ] [
            div [ _class "control"] [
                button [
                    _class "button is-primary"
                    _type "submit"
                ] [ str okText]
            ]
            div [ _class "control"] [
                a [
                    _class "button is-light"
                    _href cancelHref
                ] [ str cancelText]
            ]
        ]
        
module Inputs =

    type Metadata =
        { Id: string
          Label: string
          Name: string }


    let textInput { Id = id; Label = labelText; Name = name } value =
        div [ _class "field"; _id id ] [
            label [ _class "label" ] [ str labelText ]
            div [ _class "control" ] [
                input [ _class "input"; _type "text"; _name name; match value with Some v -> _value v | None -> () ]
            ]
        ]

    let okTextInput { Id = id; Label = labelText; Name = name } value =
        div [ _class "field"; _id id ] [
            label [ _class "label" ] [ str labelText ]
            div [ _class "control" ] [
                input [ _class "input is-success"; _type "text"; _name name; _value value ]
            ]
        ]

    let errorTextInput { Id = id; Label = labelText; Name = name } value errorMsg =
        div [ _class "field"; _id id ] [
            label [ _class "label" ] [ str labelText ]
            div [ _class "control" ] [
                input [ _class "input is-danger"; _type "text"; _name name; _value value ]
            ]  
            p [ _class "help is-danger" ] [ str errorMsg ]
        ]

    let textareaInput { Id = id; Label = labelText; Name = name } value =
        div [ _class "field"; _id id ] [
            label [ _class "label" ] [ str labelText ]
            div [ _class "control" ] [
                textarea [ _class "input"; _type "text"; _name name ] [ match value with Some v -> str v | None -> () ]
            ]
        ]

    let okTextareaInput { Id = id; Label = labelText; Name = name } value =
        div [ _class "field"; _id id ] [
            label [ _class "label" ] [ str labelText ]
            div [ _class "control" ] [
                textarea [ _class "input is-success"; _type "text"; _name name ] [ str value ]
            ]
        ]

    let errorTextareaInput { Id = id; Label = labelText; Name = name } value errorMsg =
        div [ _class "field"; _id id ] [
            label [ _class "label" ] [ str labelText ]
            div [ _class "control" ] [
                textarea [ _class "input is-danger"; _type "text"; _name name ] [ str value ] 
            ]  
            p [ _class "help is-danger" ] [ str errorMsg ]
        ]

module Icons =
    open Giraffe.ViewEngine
    let plusIcon = 
        span [ _class "icon" ] [ 
            i [ _class "fas fa-plus" ] [ ] 
        ]
