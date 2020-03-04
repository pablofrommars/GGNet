open System

open NodaTime;

open GGNet

[<EntryPoint>]
let main argv =

    let source = new Source<Datasets.Tip.Point>(Datasets.Tip.Load())

    let limits = Plot.Limits(max = Nullable 1.0);
    let limits2 = Plot.Limits(max = Nullable (new LocalDate()));

    let theme = Theme.Default()
    theme.Axis.Text.X.Anchor <- Anchor.``end``

    let plot = 
       Plot.New(source, x = (fun o -> o.Day), y = (fun o -> o.Avg))
           .Geom_ErrorBar(ymin = (fun o -> o.Lower), ymax = (fun o -> o.Upper), position = PositionAdjustment.Dodge)
           .Scale_Color_Discrete((fun o -> o.Sex), [| "#69b3a2"; "#404080" |])
           .YLab("Tip (%)")
           .Theme()
    0
