smoutput=: (150!:0)&'Console.WriteLine(v.ToString());return new MicroJ.A<MicroJ.JString>(0);'

NB. fib test
fibtest =: 3 : 0
  x1 =. (3!:101) 1 NB. convert to bignum
  x2 =. (3!:101) 1
  c =. 0
  while. c < (y-2) do.
    tmp =.  x1
    x1 =. x2
    x2 =. tmp + x1
    c=.c+1
  end.
  x2
)

NB. fib test
fibtestx =: 3 : 0
  x1 =. (3!:101) 1 NB. convert to bignum
  x2 =. (3!:101) 1
  for_c. i. (y-2) do. NB. !hotspot
    tmp =.  x1 
    x1 =. x2
    x2 =. tmp + x1 NB. type:BigInteger
  end.
  x2
)
