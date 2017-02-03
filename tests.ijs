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
NB. ($ '') -: 0                             NB. shape empty - $ ''
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
((3 : '1+1') 0) -: 2
((3 : 'y+1') 1) -: 2
(1 (4 : 'y+x') 1) -: 2


(_1 p: 10 20 50 100) -: 4 8 15 25                    NB. Primes less than
(_1 p: 17 37 79 101) -: 6 11 21 25  NB. Primes less than (prime arguments)

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
('abc','123') -: 'abc123'					 NB. append with string

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

NB. behead empty table
(# (}. ,1)) -: 0

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

NB. from
(0 2 { 3 5 6 7) -: 3 6
(0 2 { 3 2 $ 2 # 1 2 3) -: '1 1\n3 3'
(0 2 { 3 5 6 7) -: 3 6
($ 0 { i. 5) -: ''

NB. grade
(/: ~ 4 3 1 2) -: 1 2 3 4

NB. square
((*: & *: & *: ) 4) -: 65536

((+: & +: & +: ) 4) -: 32

((+: & +: & *: ) 4) -: 64

NB. monadic train
((+/ % #) i. 10) -: 4.5

NB. train with adverb

(({.;#) /. ~ 1 1 2 2 2) -: 0 : 0
+-+-+
|1|2|
+-+-+
|2|3|
+-+-+
)

NB. rank2ex
1 0 1 0 -: ((3 $ 'abc') -:"1 (4 3 $ 'abcxyz'))

NB. nub
(~. (1 2 3 1)) -: (1 2 3)

(~. ('abc';123;'abc')) -: ('abc';123)

(,. 1 2 3) -: 0 : 0
1
2
3
)


NB. right
1 ] 3 -: 3
((1 + ]) 5) -: 6

((1,])/i.3) -: 1 1 2

NB. stitch

(1 ,. 2) -: 1 2
((i.3) ,. (1+i.3)) -: 0 : 0
0 1
1 2
2 3
)

($ 9,. (i.3)) -: '3 2'
($ 9,. (i.3 3)) -: '3 4'

NB. stich with atom
(9 ,. (i.2)) -: 0 : 0
9 0
9 1
)

NB. fork in right tine
$ (1 , (1,])) /(i. 10) -: 19

NB. cap
(1 ([: ] +) 5) -: 6
(1 ([: *: +) 5) -: 36

NB.  curtail
(}: i. 5) -: 0 1 2 3


NB. table type
NB. tables - flipping dictionary into a table
(flip ('abc';'xyx');(i.3);(1+i.3)) -: 0 : 0
+---+---+
|abc|xyx|
|---+---|
|0  |1  |
|---+---|
|1  |2  |
|---+---|
|2  |3  |
+---+---+
)

NB. tables - subsetting a table
(2 {. flip ('abc';'xyx');(i.3);(1+i.3)) -: 0 : 0
+---+---+
|abc|xyx|
|---+---|
|0  |1  |
|---+---|
|1  |2  |
+---+---+
)

NB. tables - taking the last row of a table
(_1 {. flip ('abc';'xyx');(i.3);(1+i.3)) -: 0 : 0
+---+---+
|abc|xyx|
|---+---|
|2  |3  |
+---+---+
)

('abc' /: flip ('abc';'xyx');(2 1 3);(1+i.3)) -: 0 : 0
+---+---+
|abc|xyx|
|---+---|
|1  |2  |
|---+---|
|2  |1  |
|---+---|
|3  |3  |
+---+---+
)

NB. tables - sort by column name
('xyx' /: flip ('abc';'xyx');(2 1 3);(1+i.3)) -: 0 : 0
+---+---+
|abc|xyx|
|---+---|
|2  |1  |
|---+---|
|1  |2  |
|---+---|
|3  |3  |
+---+---+
)


NB. tables - select column
((<'abc') { flip ('abc';'xyz');(1,2);(2,3)) -: 0 : 0
+---+
|abc|
|---|
|1  |
|---|
|2  |
+---+
)

NB. tables - select column and row
(0 { (<'abc') { flip ('abc';'xyz');(1,2);(2,3)) -: 0 : 0
+---+
|abc|
|---|
|1  |
+---+
)

NB. tables - update a column
((flip (<'abc');(1,2)),(flip (<'abc');(3,4))) -: 0 : 0
+---+
|abc|
|---|
|3  |
|---|
|4  |
+---+
)

NB. tables - add a column
((flip (<'abc');(1,2)),(flip (<'xyz');(3,4))) -: 0 : 0
+---+---+
|abc|xyz|
|---+---|
|1  |3  |
|---+---|
|2  |4  |
+---+---+
)

NB. tables - add a calculated column
((flip ('a';'b');(1,2);(3,5)),('c';'a + b')) -: 0 : 0
+-+-+-+
|a|b|c|
|-+-+-|
|1|3|4|
|-+-+-|
|2|5|7|
+-+-+-+
)

NB. table - filter
('a > 1' { (flip ('a';'b');(i. 4);(5+i. 4))) -: 0 : 0
+-+-+
|a|b|
|-+-|
|2|7|
|-+-|
|3|8|
+-+-+
)

NB. table - filter and sort
('a' \: 'a > 1' { (flip ('a';'b');(i. 4);(5+i. 4))) -: 0 : 0
+-+-+
|a|b|
|-+-|
|3|8|
|-+-|
|2|7|
+-+-+
)

NB. behead a table with a single column
(+/ }. (flip (<'a');(i.10))) -: 45

NB. behead a table with multiple columns

(}. (flip ('a';'b');(i. 5);(100+ i.5))) -: 0 : 0
+---------+-------------------+
|0 1 2 3 4|100 101 102 103 104|
+---------+-------------------+
)



NB. sort
 (0 2 1 /: ('a';'c';'b') ) -: ('a';'b';'c')


NB. table amend, first column as key
(1 ('v';'b') } flip ('k';'v');(> 'a';'b';'c');(0 0 0)) -: 0 : 0
+-+-+
|k|v|
|-+-|
|a|0|
|-+-|
|b|1|
|-+-|
|c|0|
+-+-+
)

NB. table amend, arbitrary key
(3 ('v';0;'v')  } (1 ('v';'b') } flip ('k';'v');(> 'a';'b';'c');(0 0 0))) -: 0 : 0
+-+-+
|k|v|
|-+-|
|a|3|
|-+-|
|b|1|
|-+-|
|c|3|
+-+-+
)

NB. table - footer
((('k';'# k'),.('v';'+/ v')) _1 } flip ('k';'v');(> 'a';'b';'c');(5 10 15)) -: 0 : 0
+-+--+
|k|v |
|-+--|
|a|5 |
|-+--|
|b|10|
|-+--|
|c|15|
|-+--|
|3|30|
+-+--+
)

NB. table - key without expressions
((<'k') ] /. flip ('k';'v');( 10 1 $ 'ab');(i.10)) -: 0 : 0
+-+-+
|k|v|
|-+-|
|a|5|
|-+-|
|b|5|
+-+-+
)

NB. table - key without expressions
((<'k') ] /. flip ('k';'v');( 10 1 $ 'ab');(i.10)) -: 0 : 0
+-+-+
|k|v|
|-+-|
|a|5|
|-+-|
|b|5|
+-+-+
)

NB. table - key filtered table
((<'k') ] /. ('k= ''a'' ' { flip ('k';'v');( 10 1 $ 'ab');(i.10))) -: 0 : 0
+-+-+
|k|v|
|-+-|
|a|5|
+-+-+
)


NB. table key with footer expression
(('k';'k2') ] /. (('v';'+/ v') _1 } flip ('k';'k2';'v');(5 1 $ 'ab');(> 'a';'a';'a';'b';'b');(5$5))) -: 0 : 0
+-+--+--+
|k|k2|v |
|-+--+--|
|a|a |10|
|-+--+--|
|b|a |5 |
|-+--+--|
|b|b |5 |
|-+--+--|
|a|b |5 |
+-+--+--+
)

NB. table key with expressions
((<'k') ('+/ v';'{. k2') /. flip ('k';'k2';'v');(5 1 $ 'ab');(> 'a';'a';'a';'b';'b');(i.5)) -: 0 : 0
+-+----+-----+
|k|+/ v|{. k2|
|-+----+-----|
|a|6   |a    |
|-+----+-----|
|b|4   |a    |
+-+----+-----+
)

NB. amend / join with table

1 [ ages=. (1+i. 120)
1 [ ageBin =. 10 # 10 20 30 40 50 60 70 80 90 100 110 120
1 [ ageBinTable =. flip ('age';'ageBin');ages;ageBin
1 [ ageTable =. flip (<'age');(1 35 85)

ageBinTable (<'age') } ageTable -: 0 : 0
+---+------+
|age|ageBin|
|---+------|
|1  |10    |
|---+------|
|35 |40    |
|---+------|
|85 |90    |
+---+------+
)

NB. amend with table, missing values
1 [ ages=. (120#1)
1 [ ageBinTable =. flip ('age';'ageBin');ages;ageBin
ageBinTable (<'age') } ageTable -: 0 : 0
+---+------+
|age|ageBin|
|---+------|
|1  |120   |
|---+------|
|35 |0     |
|---+------|
|85 |0     |
+---+------+
)

NB. unbox a single element
(> (<'foo')) -: 'foo'

NB. multiple assignments
( a + b [ 'a b' =: 1 2) -: 3

(abc [ 'abc efg' =: ('abc';'efg')) -: 'abc'


NB. if control structure
((3 : 'if. y > 1 do. 1 else. 0 end.') 2) -: 1
((3 : 'if. y > 2 do. 1 else. 100 end.') 2) -: 100
((3 : 'if. y < 2 do. ''a'' elseif. y < 100 do. ''b'' else. ''c'' end.') 50) -: 'b'

NB. if in explicit

iftest =: 3 : 0
if. y < 2 do. 'a'
elseif. y < 100 do. 'b'
else. 'c'
end.
)

(iftest 1) -: 'a'
(iftest 50) -: 'b'
(iftest 101) -: 'c'

iftest =: 3 : 0
if. y < 2 do.
'z'
'a'
elseif. y < 100 do.
'z'
'b'
else.
'z'
'c'
end.
)

(iftest 1) -: 'a'
(iftest 50) -: 'b'
(iftest 101) -: 'c'


NB. iftest immediate
iftest =: 3 : 0
if. y < 2 do. 'a' return.
elseif. y < 100 do. 'b' return. end.
elseif. y > 100 do. 'c' end.
'NO'
)

(iftest 1) -: 'a'
(iftest 50) -: 'b'
(iftest 200) -: 'NO'

((<<0) { (('ukey';'a') } flip (('a';'b');(i.10);(10 $ 1 2)))) -: 0 : 0
+-+-+
|a|b|
|-+-|
|0|1|
+-+-+
)

NB. behead with key
(, > }. (<<0) { (('ukey';'a') } flip (('a';'b');(i.10);(10 $ 1 2)))) -: 0 1

(}. (<'a') { (<<0) { (('ukey';'a') } flip (('a';'b');(i.10);(10 $ 1 2)))) -: 0


((<<500) { (('ukey';'a') } flip (('a';'b');(i.10);(10 $ 1 2)))) -: 0 : 0
+-+-+
|a|b|
+-+-+
)

NB. key with  multiple rows
((<<1) { (('key';'b') } flip (('a';'b');(i.10);(10 $ 1 2)))) -: 0 : 0
+-+-+
|a|b|
|-+-|
|0|1|
|-+-|
|2|1|
|-+-|
|4|1|
|-+-|
|6|1|
|-+-|
|8|1|
+-+-+
)



NB. I.
(2 3 I. 1) -: 0
(2 3 I. 2) -: 0
(2 3 I. 3) -: 1
(2 3 I. 1 2 3) -: 0 0 1


NB. rank on table
((3 : '# y') (flip ('a';'b');(i.10);(10 $ 1 2))) -: 10
($ (3 : '# y') (flip ('a';'b');(i.10);(10 $ 1 2))) -: ''
($ (3 : '# y') "1 (flip ('a';'b');(i.10);(10 $ 1 2))) -: 10

NB. return a box from each row of a table
((3 : ' 1;2 ') "1 (flip ('a';'b');(i.2);(2 $ 1 2))) -: 0 : 0
+-+-+
|1|2|
+-+-+
|1|2|
+-+-+
)
NB. return a new table per row, with a value from the original table
((3 : ' flip (''x'';''z'');0;(}. (<1) { y) ') "1 (flip ('a';'b');(i.2);(2 2 $ 'abc'))) -: 0 : 0
+-+--+
|x|z |
|-+--|
|0|ab|
|-+--|
|0|ca|
+-+--+
)

NB. return a new table per row, with a value from the original table
((3 : ' (''a'';a+1),.(''b'';''c'') ') "1 (flip ('a';'b');(i.2);(2 2 $ 'abc'))) -: 0 : 0
+-+-+
|a|b|
|-+-|
|1|c|
|-+-|
|2|c|
+-+-+
)

NB. insert or {. on table
('+/a' /. (flip ('a';'b');(i.10);(10 $ 1 2))) -: 45
(+/ ((<'a') {. (flip ('a';'b');(i.10);(10 $ 1 2)))) -: 45

({. ((<'a') {. 0 { (flip ('a';'b');(i.10);(10 $ 1 2)))) = 0
(((<'a') {. 0 { (flip ('a';'b');(i.10);(10 $ 1 2)))) = 0


NB. importance of vectorized calcs
NB. 6!:2 ' z=: ''extended_price % qty_shipped'' / InvoiceAgg '
NB. 0.0194236
NB. 6!:2 ' z2=: ( <''ASP'' ; ''extended_price % qty_shipped'') { InvoiceAgg '
NB. 4.7938795

NB. vectorized calculated columns
('a+b' / (flip ('a';'b');(i.3);(3 $ 1 2))) -: (1 3 3)

NB. create a column through rank
((3 : 'flip (<''c'');(a+b))') "1 (flip ('a';'b');(i.3);(3 $ 1 2))) -: 0 : 0
+-+
|c|
|-|
|1|
|-|
|3|
|-|
|3|
+-+
)

NB. create a column through from
(((<'c';'a+b')) { (flip ('a';'b');(i.3);(3 $ 1 2))) -: 0 : 0
+-+
|c|
|-|
|1|
|-|
|3|
|-|
|3|
+-+
)

NB. create a column through addcol (fastest)
((('addcol';'a+b') } (flip ('a';'b');(i.3);(3 $ 1 2)))) -: 0 : 0
+-+-+---+
|a|b|a+b|
|-+-+---|
|0|1|1  |
|-+-+---|
|1|2|3  |
|-+-+---|
|2|1|3  |
+-+-+---+
)


add=: 3 : 0
a+1
)

((3 : ' (''x''; add a ) ,. (''z''; 2 * add a)') "1 (flip ('a';'b');(i.2);(2 2 $ 'abc'))) -: 0 : 0
+-+-+
|x|z|
|-+-|
|1|2|
|-+-|
|2|4|
+-+-+
)


NB. abs
(| _5 10 _20) -: (5 10 20)

(3!:103 flip ('a';'b';'name');(i.3);(3 $ 1 2);(3 4 $ 'abc')) -: 0 : 0
[{"a":0,"b":1,"name":"abca"},{"a":1,"b":2,"name":"bcab"},{"a":2,"b":1,"name":"cabc"}]
)

((3!:102) '[{"a":0,"b":1,"name":"abca"},{"a":1,"b":2,"name":"bcab"},{"a":2,"b":1,"name":"cabc"}]') -: 0 : 0
+-+-+----+
|a|b|name|
|-+-+----|
|0|1|abca|
|-+-+----|
|1|2|bcab|
|-+-+----|
|2|1|cabc|
+-+-+----+
)

NB. json type promotion
((3!:102) '[{"a":1},{"a":1.2}]') -: 0 : 0
+---+
|a  |
|---|
|1  |
|---|
|1.2|
+---+
)

(~. flip ('a';'b');(4 2 $ 'ab');(4 $ (1,2))) -: 0 : 0
+--+-+
|a |b|
|--+-|
|ab|1|
|--+-|
|ab|2|
+--+-+
)

(~. (<'a') { flip ('a';'b');(4 2 $ 'ab');(4 $ (1,2))) -: 0 : 0
+--+
|a |
|--|
|ab|
+--+
)

NB. AND
NB. (0 *. 1) -: 0


((4!:0) ;: 'qqq 123') -: _1 0


NB. decimal types
((9!:100) 1) -: 1
(1+3)-:4
(2*6)-:12
(1%4)-:0.25

(; (1;2)) -: (1,2)

((9!:100) 0) -: 0


NB. rank support on trains
(({. ,. }.)"1 (i. 3 2)) -: 0 : 0
0 1

2 3

4 5
)

NB. TODO support ,/ i. 4 3 2

NB. ('id';'col1';'col2');(> ;: 'a b c');(i.3);(i.3)
NB. ([: ,/ ({. ,. }.)"1) (flip ('id';'col1';'col2');(> ;: 'a b c');(i.3);(i.3))


NB. default value for missing column
((<'b') { !. 0 (3!:102) '[{a:"1"}]') -: 0 : 0
+-+
|b|
|-|
|0|
+-+
)

NB. found column
((<'a') { (3!:102) '[{a:"1"}]') -: 0 : 0
+-+
|a|
|-|
|1|
+-+
)


NB. non-matching row should not blow up
('b = ''xx'' ' { (<<4) {('ukey';'a') } ((3!:102) '[{"a":1,"b":"xyz"},{"a":1.2, "b":"abc"}]')) -: 0 : 0
+-+-+
|a|b|
+-+-+
)

NB. TODO add test for this keyword on rank1table


NB. drop on string
(5 }. '2016-01-05') -: '01-05'

NB. negative index on string
(_2 {. '2016-01-05') -: '05'

NB. from bugfix
($ 1 { ('foo';'bar'),.('foo2';'abc')) -: (,2)

NB. or
(1 +. 0) -: 1
(0 +. 1) -: 1
(1 +. 1) -: 1
(0 +. 0) -: 0
(('foo' -: 'foo') +. (6 < 5)) -: 1

NB. special code to prevent infinity
((3,2,4) (%)^:(0~:])"0 (6,0,3)) -: 0.5 0 1.333333

NB. and
(1 *. 0) -: 0
(0 *. 1) -: 0
(1 *. 1) -: 1
(0 *. 0) -: 0
(('foo' -: 'foo') *. (6 < 5)) -: 0
(('foo' -: 'foo') *. (5 < 6)) -: 1


NB. bug fix with math mixed and double/long, was returning 1
(((3!:102) (<'a');(1.2 2.3 3.4)),('b';'a < 2')) -: 0 : 0
+---+-+
|a  |b|
|---+-|
|1.2|1|
|---+-|
|2.3|0|
|---+-|
|3.4|0|
+---+-+
)

NB. dyadic & and [

(2 (<&0@[) 1) -: 0
(2 (<&0@[) _1) -: 0
(_5 (<&0@[) 2) -: 1

(_1 (<&0@[ +. >) 3) -: 1
(4 (<&0@[ +. >) 3) -: 1
(2 (<&0@[ +. >) 3) -: 0


NB. less than equal to
(1 <: 1) -: 1
(0 <: 1) -: 1
(1.01 <: 2) -: 1
(1.01 <: 1.01) -: 1
(1.01 <: 1.00) -: 0

NB. greater than equal to
(1 >: 1) -: 1
(0 >: 1) -: 0
(1.01 >: 2) -: 0
(1.01 >: 1.01) -: 1
(1.01 >: 1.00) -: 1


NB. deviation from J.. concatenate strings of different lengths
((>('hi';'bye')) & ' joe') -: (>('hi joe';'bye joe'))
('hi' & ' joe') -: 'hi joe'
('hi ' & (>'joe';'bob')) -: (>'hi joe';'hi bob')


NB. running sum
(+/\ 1 2 3 4) -: 1 3 6 10

NB. approximate match

NB. amend / join with table


1 [ ageBinTable =. flip ('ageBin';'class');(10 20 60 150);(>('kid';'teenager';'middle age';'elderly'))
1 [ ageTable =. flip (<'age');(1 35 85)

(ageBinTable ('age';'ageBin') } !. 'approx' ageTable) -: 0 : 0
+---+------+----------+
|age|ageBin|class     |
|---+------+----------|
|1  |10    |kid       |
|---+------+----------|
|35 |60    |middle age|
|---+------+----------|
|85 |150   |elderly   |
+---+------+----------+
)

((<'newcol is col') { flip (<'col');(1 2 3)) -: 0 : 0
+------+
|newcol|
|------|
|1     |
|------|
|2     |
|------|
|3     |
+------+
)


NB. bug fix count on string col
((<'k') ('# ~. k2';'{. k2') /. flip ('k';'k2';'v');(5 1 $ 'ab');(> 'aa';'a';'a';'b';'b');(i.5)) -: 0 : 0
+-+-------+-----+
|k|# ~. k2|{. k2|
|-+-------+-----|
|a|3      |aa   |
|-+-------+-----|
|b|2      |a    |
+-+-------+-----+
)



(# region1 [ ('region1 region2' =: (('Region';'Region') {. (<<'joe') { ('ukey';'User') } (3!:102) '[{User:"joe", Region:""}, {User:"bob",Region:"ABC"}]'))) -: 0

(((<'User') (<'# (~. Region)') /. (3!:102) '[{User:"joe", Region:"AAA"}, {User:"joe",Region:"BBB"}, {User:"joe",Region:"CCC"}]')) -: 0 : 0
+----+-------------+
|User|# (~. Region)|
|----+-------------|
|joe |3            |
+----+-------------+
)

(# ~. (1 3 $ 'foo')) -: 1
NB. (# ~. (3 $ 'foo')) -: 2
NB. (# ~. ('foo')) -: 2

(((<'User') (<'# (~. Region)') /. (3!:102) '[{User:"u1", Region:"AAA"}, {User:"u2",Region:"BBB"}, {User:"u3",Region:"CCC"}]')) -: 0 : 0
+----+-------------+
|User|# (~. Region)|
|----+-------------|
|u1  |1            |
|----+-------------|
|u2  |1            |
|----+-------------|
|u3  |1            |
+----+-------------+
)

(". '1 2 3') -: 1 2 3
(+/  ('". col' / (3!:102) (<'col');(>'1';'2';'3'))) -: 6
(". !. 'NaN' '1 x 3') -: 1 _ 3

(<"1 ((>('abc';'123';'xyz')) & ' v')) -: 0 : 0
+-----+-----+-----+
|abc v|123 v|xyz v|
+-----+-----+-----+
)



 ('# k2' / flip ('k';'k2';'v');(3 1 $ 'ab');(> 'aaa';'a';'bb');(i.3)) -: 3 1 2


(%: 4) -: 2

NB. sqrt test with decimal
(9!:100) 1
(%: 4) -: 2
((9!:100) 0) -: 0

NB. i. with rank2
($ i. (2 3) $ 2 3 4 2 3 4) -: 2 2 3 4

NB. anagram (monadic)
(A. 0 1 2) -: 0
(A. 2 1 0) -: 5
(A. 3 4 0 1) -: 64
(A. (2 3) $ 0 2 1) -: 1 1

NB. ". for eval
evaltest =: 3 : 0
A=. i.3
". 'A'
)
(evaltest 0) -: (i.3)

NB. union table
(((3!:102) (<'a');1) ,: ((3!:102) (<'a');2)) -: 0 : 0
+-+
|a|
|-|
|1|
|-|
|2|
+-+
)

NB. union table (different sequence of columns)
(((3!:102) ('a';'b');1;2) ,: ((3!:102) ('b';'a');4;3)) -: 0 : 0
+-+-+
|a|b|
|-+-|
|1|2|
|-+-|
|3|4|
+-+-+
)

NB. aggregate table with column headers
(_ ('a is +/ a';'ct is _N') /. (flip ('a';'b');(i.10);(10 $ 1 2))) -: 0 : 0
+--+--+
|a |ct|
|--+--|
|45|10|
+--+--+
)