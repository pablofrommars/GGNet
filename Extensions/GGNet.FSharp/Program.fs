open GGNet
open GGNet.Datasets

[<EntryPoint>]
let main argv =

    let plot = 
       Plot.New(Tip.Load(), x = (fun o -> o.Day), y = (fun o -> o.Avg))
           .Geom_ErrorBar(ymin = (fun o -> o.Lower), ymax = (fun o -> o.Upper), position = PositionAdjustment.Dodge)
           .Scale_Color_Discrete((fun o -> o.Sex), [| "#69b3a2"; "#404080" |])
           .YLab("Tip (%)")
           .Theme()
    0
