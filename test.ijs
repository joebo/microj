NB. |CompAcct|CostAllowance|ExtCost|ExtPriceNoFunds|InvDateYYYYMMDD|QtyShipped|UniqueID|CompItem|CustProdKey|InvoiceYearMonth|ShiptoState|

PATH =: 'c:/d3/flooring/haines/d3db/'
0 [ tblx =: ('autosym';1) (151!:3) (<PATH)
NB. ('addcol';'InvoiceYearMonth is left(InvDateYYYYMMDD;4)') } tblx
NB. ('addcol';'CustProdKey is CompAcct & ''|'' & CompItem ') } tblx
NB. PATH s: tblx

NB. 0 [ agg =: (<'CustProdKey') ('+/ ExtPriceNoFunds';'+/ ExtCost' ) /. !. 'noparse' tblx

NB. aggregate('CustProdKey';(<'foo is +/ ExtPriceNoFunds');tblx)

NB. aggregate('InvoiceYearMonth';(<'foo is +/ ExtPriceNoFunds');tblx)
NB. agg =: (<'InvoiceYearMonth') (<'+/ ExtPriceNoFunds') /. !. 'noparse' tblx

NB. agg =: (<'CustProdKey') (<'+/ ExtPriceNoFunds') /. !. 'noparse' tblx

NB. aggregate('InvoiceYearMonth';(<'foo is +/ ExtPriceNoFunds');tblx)
NB. # aggregate('CustProdKey';(<'foo is +/ ExtPriceNoFunds');tblx)
10 {. aggregate('CustProdKey';('foo is +/ ExtPriceNoFunds';'Month is {. CompAcct';'Foo is {. CompItem');tblx)



# aggregate(('CompAcct';'CompAcct');(<'foo is +/ ExtPriceNoFunds');tblx)
