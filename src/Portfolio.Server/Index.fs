namespace Portfolio.Server

open Bolero
open Bolero.Html
open Bolero.Server.Html
open Portfolio
open System.Web

module Index =
    let page = doctypeHtml [] [
        head [] [
            meta [attr.charset "UTF-8"]
            meta [attr.name "viewport"; attr.content "width=device-width, initial-scale=1.0"]
            title [] [text "Portfolio"]
            ``base`` [attr.href "/"]
            link [attr.rel "stylesheet"; attr.href "css/portfolio.css"]
            link [attr.rel "stylesheet"; attr.href "css/jquery.pagepiling.css"]
            link [attr.rel "stylesheet"; attr.href "css/lightbox.css"]
            link [attr.rel "stylesheet"; attr.href "https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.15.3/css/all.min.css"]
            script [attr.src "javascript/devicedetection.js"] []
            script [attr.src "https://ajax.googleapis.com/ajax/libs/jquery/3.6.0/jquery.min.js"] []
        ]
        body [] [
            div [attr.id "main"] [rootComp<Client.Main.MyApp>]
            boleroScript
        ]
    ]
