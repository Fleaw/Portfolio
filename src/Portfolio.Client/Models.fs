namespace Portfolio.Client

module Models =

    type Game =
        {
            Id: int
            Title: string
            Description: string
            Url: string
            Cover: string
            Screenshots: string list
        }

    type GitHubRepo =
        {
            Name : string
            Description : string option
            Topics : string list
            Languages : string list
            Url : string
        }