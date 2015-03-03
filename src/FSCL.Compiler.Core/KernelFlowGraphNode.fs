﻿namespace FSCL.Compiler
open System.Collections.Generic
open System.Reflection
open Microsoft.FSharp.Quotations
open System
open System.Collections.Generic
open System.Collections.ObjectModel
   
[<AllowNullLiteral>]
type IKFGNode =  
    abstract member Input: IReadOnlyList<IKFGNode> with get
    abstract member OutputType: Type with get
    
[<AllowNullLiteral>]
type IKFGDataNode =
    inherit IKFGNode
    abstract member Data: Expr with get
    
[<AllowNullLiteral>]
type IKFGOutValNode =
    inherit IKFGNode
    abstract member Data: Expr with get
    
[<AllowNullLiteral>]
type IKFGEnvVarNode =
    inherit IKFGNode
    abstract member Var: Var with get
    
[<AllowNullLiteral>]
type IKFGCollectionVarNode =
    inherit IKFGNode
    abstract member Var: Var with get

[<AllowNullLiteral>]
type IKFGKernelNode =
    inherit IKFGNode
    abstract member Module: IKernelModule with get
    
[<AllowNullLiteral>]
type IKFGSequentialFunctionNode =
    inherit IKFGNode
    abstract member Expr: Expr with get
    abstract member Instance: Expr option with get
    abstract member MethodInfo: MethodInfo with get
    
[<AllowNullLiteral>]
type IKFGCollectionCompositionNode =
    inherit IKFGNode
    abstract member CompositionID: MethodInfo with get
    abstract member SubGraph: IKFGNode with get
    abstract member CollectionVars: Var list with get
    
[<AllowNullLiteral; AbstractClass>]
type KFGNode() =
    interface IKFGNode with 
        member this.Input 
            with get() =
                this.InputNodes :> IReadOnlyList<IKFGNode>
        member this.OutputType
            with get() =
                this.OutputType
    member val InputNodes = new List<IKFGNode>()
        with get
    abstract member OutputType: Type with get
        
[<AllowNullLiteral>]
type KFGDataNode(data:Expr) =
    inherit KFGNode() 
    override this.OutputType 
        with get() =
            data.Type
    interface IKFGDataNode with
        override this.Data 
            with get() =
                this.Data
    member val Data = data with get
        
[<AllowNullLiteral>]
type KFGOutValNode(data:Expr) =
    inherit KFGNode()
    override this.OutputType 
        with get() =
            data.Type
    interface IKFGOutValNode with
        override this.Data 
            with get() =
                this.Data
    member val Data = data with get
        
[<AllowNullLiteral>]
type KFGEnvVarNode(v:Var) =
    inherit KFGNode()
    override this.OutputType 
        with get() =
            v.Type
    interface IKFGEnvVarNode with
        override this.Var 
            with get() =
                this.Var
    member val Var = v with get
        
[<AllowNullLiteral>]
type KFGCollectionVarNode(v:Var) =
    inherit KFGNode()
    override this.OutputType 
        with get() =
            v.Type
    interface IKFGCollectionVarNode with
        override this.Var 
            with get() =
                this.Var
    member val Var = v with get
        
[<AllowNullLiteral>]
type KFGKernelNode(m:IKernelModule) =
    inherit KFGNode()
    override this.OutputType 
        with get() =
            m.Kernel.OriginalBody.Type
    interface IKFGKernelNode with
        override this.Module 
            with get() =
                this.Module
    member val Module = m with get
                
[<AllowNullLiteral>]
type KFGSequentialFunctionNode(o, mi:MethodInfo, e) =
    inherit KFGNode()
    override this.OutputType 
        with get() =
            mi.ReturnType
    interface IKFGSequentialFunctionNode with
        override this.Expr 
            with get() =
                this.Expr
        override this.Instance 
            with get() =
                this.Instance
        override this.MethodInfo 
            with get() =
                this.MethodInfo
    member val Expr = e with get
    member val Instance = o with get
    member val MethodInfo = mi with get
    
[<AllowNullLiteral>]
type KFGCollectionCompositionNode(mi:MethodInfo, sg, vars) =
    inherit KFGNode()
    override this.OutputType 
        with get() =
            mi.ReturnType
    interface IKFGCollectionCompositionNode with
        override this.CompositionID 
            with get() =
                this.CompositionID
        override this.CollectionVars 
            with get() =
                this.CollectionVars
        override this.SubGraph 
            with get() =
                this.SubGraph
    member val CompositionID = mi with get
    member val SubGraph = sg with get
    member val CollectionVars = vars with get

[<AllowNullLiteral>]
[<AbstractClass>]
type IKernelExpression(root: IKFGNode) =
    member val KFGRoot = root 
        with get    
    member val OutputType = root.OutputType 
        with get
    abstract member KernelModules: IReadOnlyList<IKernelModule>
    abstract member KernelModulesCompiled: IReadOnlyList<KernelModule>
    
[<AllowNullLiteral>]
type KernelExpression(root: IKFGNode) =
                      //metadataVerifier: ReadOnlyMetaCollection * ReadOnlyMetaCollection -> bool) =
    inherit IKernelExpression(root)

    //let kmods = new Dictionary<FunctionInfoID, List<ReadOnlyMetaCollection * KernelModule>>()
    let compileKmList = new List<KernelModule>()
    //let copyKmList = new List<KernelModule>()
    let fullKmList = new List<IKernelModule>()
    let rec graphSearch(r: IKFGNode) =
        match r with
        | :? KFGKernelNode ->
            let km = (r :?> IKFGKernelNode).Module :?> KernelModule
//            if kmods.ContainsKey(km.Kernel.ID) then
//                let potentialKernels = kmods.[km.Kernel.ID]
//                let item = Seq.tryFind(fun (cachedMeta: ReadOnlyMetaCollection, cachedKernel: KernelModule) ->
//                                            metadataVerifier(cachedMeta, km.Kernel.Meta)) potentialKernels
//                match item with
//                | Some(m, k) ->
//                    copyKmList.Add(km)
//                | _ ->
//                    kmods.[km.Kernel.ID].Add(km.Kernel.Meta, km)
//                    compileKmList.Add(km)
//            else
//                kmods.Add(km.Kernel.ID, new List<ReadOnlyMetaCollection * KernelModule>())
//                kmods.[km.Kernel.ID].Add(km.Kernel.Meta, km)
//                compileKmList.Add(km)
            fullKmList.Add(km)
        | :? KFGCollectionCompositionNode ->
            (r :?> IKFGCollectionCompositionNode).SubGraph |> graphSearch 
        | _ ->
            ()        
        for item in r.Input do
            graphSearch(item)
    do
        graphSearch(root)
                 
    override val KernelModules = fullKmList :> IReadOnlyList<IKernelModule>
        with get   
    override this.KernelModulesCompiled 
        with get() =
            this.KernelModulesRequiringCompilation :> IReadOnlyList<KernelModule>
    member val KernelModulesRequiringCompilation = compileKmList 
        with get   
//    member val KernelModulesCopiedFromCache = copyKmList 
//        with get   