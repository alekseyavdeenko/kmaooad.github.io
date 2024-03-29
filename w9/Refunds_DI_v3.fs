module RefundsV3

open System
open FSharp.Data
open FSharp.Json


module DB =
    let insert<'T> _ _ = ()
    let select<'T> _: 'T list = []

module Domain =
    type Refund =
        { amount: decimal
          customer: int
          date: DateTime }

    type BillingPeriod =
        { from: DateTime
          till: DateTime }

module Billing =

    type Payment =
        { customer_id: int
          date: DateTime
          amount: decimal }


    let refundsCreatorImpl payments =
        let norm a = -a

        let rr =
            seq {
                for p in payments do
                    if p.amount < 0m then
                        yield { Domain.Refund.amount = norm p.amount
                                Domain.Refund.customer = p.customer_id
                                Domain.Refund.date = p.date }
            }
        rr |> List.ofSeq

    let refundRepositoryImpl refunds = refunds |> List.iter (DB.insert "refunds")

    let resultProcessorImpl payments iRefundsCreator iRefundsRepository =
        payments
        |> iRefundsCreator
        |> iRefundsRepository

    type IPaymentDownloader =
        abstract member Download : int -> Domain.BillingPeriod -> Payment list

    type IResultProcessor =
        abstract member Process : Payment list -> unit

    let download customer period (d: IPaymentDownloader) (p: IResultProcessor) =
        (customer, period)
        ||> d.Download 
        |> p.Process

module Config =
    [<Literal>]
    let BillingUrl = "https://fancybilling.kmaooadtech.io"

/// ------
/// Client
/// ------

module Client =
    open Billing
    open Domain


    let adjustTz (d: DateTime) = d.AddHours(-5.0)
    let format (d: DateTime) = sprintf "%O" d
    let prepare = adjustTz >> format

    let httpClientImpl url formData = Http.RequestString(url, httpMethod = "POST", body = FormValues formData)

    let requestFactoryImpl (customer: int) (p: BillingPeriod) =
        let body =
            [ ("customer_id", customer |> string)
              ("from", p.from |> prepare)
              ("to", p.till |> prepare) ]
        (Config.BillingUrl, body)

    let responseHandlerImpl (response: string): Payment list = response |> Json.deserialize<Payment list>

    let customer = 404

    let period =
        { from = DateTime(2019, 10, 1)
          till = DateTime(2019, 10, 31) }
    
    // Interfaces mocks (replace with real interfaces and add their implementations)
    type IRequestFactory = int -> BillingPeriod -> string * (string * string) list
    type IHttpClient = string -> seq<string * string> -> string
    type IResponseHandler = string -> Payment list

    type Downloader (rf: IRequestFactory, http: IHttpClient, rh: IResponseHandler) = 
        interface Billing.IPaymentDownloader with
         member this.Download c p =
            (c, p) // real parameters
            ||> rf
            ||> http
            |> rh
        

    // Code below will not work anymore - try to complete implementation yourself

    let _DI_resultProcessorImpl pp =
        pp // parameters "placeholders"
        |> resultProcessorImpl
        <|| (refundsCreatorImpl, refundRepositoryImpl) // inject dependencies

    let _DI_download c p =
        (c, p) // parameters "placeholders"
        ||> download
        <|| (downloaderImpl, _DI_resultProcessorImpl) // inject dependencies

    (customer, period) ||> _DI_download
