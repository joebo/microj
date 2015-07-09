NB. prefix lines with !B to set a breakpoint and launch debuger

(1+2) -: 3                              NB. add
(1.5 + 2.5) -: 4                        NB. add float
(4.5 + 3) -: 7.5                        NB. add float + int
1 -: 4 - 3                              NB. subtract
1.5 -: 4.5 - 3                          NB. subtract float - int
0.5 -: 4 - 3.5                          NB. subtract int - float
_1 -: 5 - 6                             NB. subtract negative
6 -: 2 * 3                              NB. multiply int
6.25 -: 2.5 * 2.5                       NB. multiply float
5 -: 2 * 2.5                            NB. multiply int*float
5 -: 10 % 2                             NB. divide int
0.25  -: 1 % 4                          NB. divide float
0 1 2 -: i. 3                           NB. iota simple
2 1 0 -: i. _3                          NB. iota simple negative
'3 2\n1 0' -: i. _2 _2                  NB. iota negative array
'2 1 0\n5 4 3\n8 7 6' -: i. 3 _3        NB. iota negative mixed
3=$ i. 3                                NB. shape iota simple
(3 3 3) -: 3 $ 3                        NB. reshape int
'3 3 3\n3 3 3' -: (2 3 $ 3)             NB. reshape int


(3 $ 3.2) -: (3.2 3.2 3.2)              NB. reshape double
(3 2 $ 'abc') -: 'ab\nca\nbc'           NB. reshape string


(+/ i. 4) -: 6                          NB. adverb simple

(+/ i. 2 3) -: 3 5 7                    NB. multi-dimensional sum
(i. 2 3) -: '0 1 2\n3 4 5'              NB. multi-dimensional


(i. 2 2 2) -: '0 1\n2 3\n\n4 5\n6 7'    NB. multi-dimensional 2
(1 + i. 2 2) -: '1 2\n3 4'              NB. multi-dimensional add
(+/ i. 2 3) -: (3 5 7)                  NB. multi-dimensional sum
(+/ i. 2 2 2) -: '4 6\n8 10'            NB. multi-dimensional sum higher rank

(+/ i. 4 3 2) -: '36 40\n44 48\n52 56'  NB. multi-dimensional sum higher rank 2
(a + a=:5) -: 10                        NB. assignment

(*/ 2 2 2) -: 8                         NB. */ int
(+/ 2.5 2.5) -: 5                       NB. +/ 2.5 2.5
(|: i. 2 3) -: '0 3\n1 4\n2 5'          NB. transpose
(3 = 3) -: 1                            NB. equals true
(3 = 2) -: 0                            NB. equals false

(3.2 = 2.2) -: 0                        NB. equals float false
(3.2 = 3.2) -: 1                        NB. equals float true
('abc' = 'abc') -: 1                    NB. equals string
('abc' = 'abb') -: 0                    NB. equals string false
(( 0 1 2 ) = i. 3) -: 1 1 1             NB. equals array
(( 0 1 3 ) = i. 3) -: 1 1 0             NB. equals array false

(1 $ 'abc') -: 'a'                      NB. 1 $ 'abc'
($ '') -: 0                             NB. shape empty - $ ''
($ 2 2 $ 'abcd') -: 2 2                 NB. shape string $ 2 2 $ 'abcd'
(# 1) -: 1                              NB. tally
(# i. 5) -: 5                           NB. tally i.
(# i. 5 4 3) -: 5                       NB. tally multidimensional
(# 0 $ 0) -: 0                          NB. tally empty
( 1 + _5 _6) -: _4 _5                   NB. negative numbers add

($/ 1 1 5) -: 5                         NB. $/ 1 1 5
($/ 5 1 1) -: 1 1 1 1 1                 NB. $/ 5 1 1
(*/ ( 1 + i. 3 )) -: 6                  NB. */ 1 + i. 3
(4 $ 'ab') -: 'abab'                    NB. 4 $ 'ab'
(0%0) -: 0                              NB. 0%0'
(1%0) -: '_'                            NB. 1%0
(a+a=:i. 2 2) -: '0 2\n4 6'             NB. array + array
(2 4 +/ 1 3) -: '3 5\n5 7'              NB. dyadic adverb call
(5 # 3) -: 3 3 3 3 3                    NB. copy 5 # 3
NB. not working (3 # i. 1 2) -: '0 1\n0 1\n0 1'         NB. copy 3 # i. 1 2
NB. not working (3 # 'ab') -: 'aaabbbccc'               NB. copy string
($"3 i. 3 2 1 ) -:  3 2 1               NB. rank shape full
($ $"3 i. 3 2 1 ) -:  3                 NB. rank shape full 1
($ $"2 i. 3 2 1) -: 3 2                 NB. rank shape - 1
($ $"1 i. 3 2 1) -: 3 2 1               NB. rank shape - 2
($ #"1 i. 3 2 1) -: 3 2                 NB. rank shape tally
(+/"1 i. 3 3) -: 3 12 21                NB. conjunction with adverb +/
(|. i.3) -: 2 1 0                       NB. reverse ints
(|. 'abc') -: 'cba'                     NB. reverse str
(|. 2 3 $ 1 2 3) -: '1 2 3\n1 2 3'      NB. reverse array
(|. i. 2 2 2) -: '4 5\n6 7\n\n0 1\n2 3' NB. reverse array

