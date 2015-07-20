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
(+/ i. 2 2 2) -: '4  6\n8 10'            NB. multi-dimensional sum higher rank

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

(1 0 1 # i. 3) -: 0 2					NB. copy with boolean
(1 0 1 # (3 3 $ 'abc')) -: 'abc\nabc'	NB. copy with strings

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

NB. 0 : 0
'abc\n123' -: 0 : 0
abc
123
)
NB. 0 conjunction 
'abc' -: (0 : 'abc')

NB. explicit verb
NB. 'abc' -: (3 : '1+1') ''
(_1 p: 10 20 50 100) -: 4 8 15 25                    NB. Primes less than
NB. Not working  (_1 p: 17 37 79 101) -: 6 11 21 25  NB. Primes less than (prime arguments)

(0 p: i. 5) -: 1 1 0 0 1                             NB. is not prime
(1 p: 2 3 17 79 199 3581) -: 1 1 1 1 1 1             NB. is prime (true)
(1 p: 10 66 111 32331 603201 9040131) -: 0 0 0 0 0 0 NB. is prime (false)
(2 p: 20) -: '2 5\n2 1'                              NB. factors with exponents
(2 p: 120) -: '2 3 5\n3 1 1'                           NB. factors with 3 exponents 
(2 p: 20 120) -: '2 5 0\n2 1 0\n\n2 3 5\n3 1 1'      NB. factors with exponents
(3 p: 56) -: 2 2 2 7                                 NB. factorization
(3 p: 56 57) -: '2  2 2 7\n3 19 0 0'                  NB. factorization w/fill
(3 p: 6973) -: 19 367                                NB. factorization
(3 p: 10111) -: 1 $ 10111                            NB. factorization (prime)
NB. == q:
(q: 567) -: 3 3 3 3 7                                NB. monadic, same as 3 p: y
(2 q: 100) -: 2 0
(10 q: 50302) -: 1 0 0 1 0 0 0 0 0 0
(30 q: 176346) -: 1 2 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 1 1 0 0 0 0

NB. == gamma (!)
(! 5) -: 120                                 NB. single arg factorial (integer)
(! 4.5) -: 52.3427777845536                  NB. single arg factorial (floating pt)

(, i. 2 2) -: 0 1 2 3                        NB. ravel list
$ ,1 -: 1                                    NB. ravel atom
(2,3 4) -: (2 3 4)                           NB. append

((i. 2 2) , (i.2 2)) -: '0 1\n2 3\n0 1\n2 3' NB. ravel rank > 1


($ 1) -: ''
($ < i. 2 2) -: ''


NB. simple box
(< i. 3) -: 0 : 0
+-----+
|0 1 2|
+-----+
)

NB. box rank > 1
(< i. 3 3) -: 0 : 0
+-----+
|0 1 2|
|3 4 5|
|6 7 8|
+-----+
)

NB. box with rank conjunction
(<"1 i. 3 2) -: 0 : 0
+---+---+---+
|0 1|2 3|4 5|
+---+---+---+
)

NB. more box
(< i. 2 1 1) -: 0 : 0
+-+
|0|
| |
|1|
+-+
)

NB. box with table
(<"0 i. 2 2 2) -: 0 : 0
+-+-+
|0|1|
+-+-+
|2|3|
+-+-+

+-+-+
|4|5|
+-+-+
|6|7|
+-+-+
)

NB. box with string
(< (2 3 $ 'abc')) -: 0 : 0
+---+
|abc|
|abc|
+---+
)

NB. box with string rank conjunction
(<"1 (2 3 $ 'abc')) -: 0 : 0
+---+---+
|abc|abc|
+---+---+
)


NB. nested boxes
((<<"0 i.2 2),(<1)) -: 0 : 0
+-----+-+
|+-+-+|1|
||0|1|| |
|+-+-+| |
||2|3|| |
|+-+-+| |
+-----+-+
)

NB. more nested boxes
((<<"0 i.2 2),(<<1)) -: 0 : 0
+-----+---+
|+-+-+|+-+|
||0|1|||1||
|+-+-+|+-+|
||2|3||   |
|+-+-+|   |
+-----+---+
)
NB. rank 2
((<"2 i. 3 3 3 3)) -: 0 : 0
+--------+--------+--------+
|0 1 2   | 9 10 11|18 19 20|
|3 4 5   |12 13 14|21 22 23|
|6 7 8   |15 16 17|24 25 26|
+--------+--------+--------+
|27 28 29|36 37 38|45 46 47|
|30 31 32|39 40 41|48 49 50|
|33 34 35|42 43 44|51 52 53|
+--------+--------+--------+
|54 55 56|63 64 65|72 73 74|
|57 58 59|66 67 68|75 76 77|
|60 61 62|69 70 71|78 79 80|
+--------+--------+--------+
)

NB. raze
(; (i.2 2)) -: '0 1 2 3'
(; (< i. 2 2)) -: '0 1\n2 3'
(; (< i. 2 2),(< i. 2 2)) -: '0 1\n2 3\n0 1\n2 3'

NB. head, take
({. i. 3 4) -: 0 1 2 3
({. 3) -: 3
({. 3,4) -: 3
(1{. 3,4) -: 3
(2{. 3,4) -: 3,4
(2{. i. 3 3) -: '0 1 2\n3 4 5'
(_1 {. i. 3 4) -: 8 9 10 11

NB. behead, drop
(}. i. 3 4) -: (2 4 $ 4 5 6 7 8 9 10 11)
(}. 3,4,5) -: 4,5
(2}. 3,4,5) -: (,5)
(_1}. i. 3 2) -: (2 2 $ 0 1 2 3)

NB. i. dyadic
(10 5 100 i. 5) -: 1
(10 5 100 i. 5 100) -: 1 2
(10 5 100 i. 10) -: 0

NB. floor
(<. 1.2 2.7) -: 1 2

NB. ceiling
(>. 1.2 2.7) -: 2 3

NB. min
(4 <. 5) -: 4

NB. max
(4 >. 5) -: 5

NB. simple reflex
+ ~ 2 -: 4

NB. key 
(< /. ~ 1 1 2 3 3) -: 0 : 0
+---+-+---+
|1 1|2|3 3|
+---+-+---+
)

(# /. ~ 1 1 2 3 3) -: 2 1 2

< /. ~ (4 3 $ 'abcxyz') -: 0 : 0
+---+---+
|abc|xyz|
|abc|xyz|
+---+---+
)

(0 2 { 3 5 6 7) -: 3 6
(0 2 { 3 2 $ 2 # 1 2 3) -: '1 1\n3 3'