make_jtable_ =: 4 : 0
   (3 !: 102) x;y
)

from =: 4 : 0
    y
)

select =: 4 : 0
    if. x -: '*' do. y return. end.
    if. (>x) -: x do. (<x) { y else. x { y end.
)

setkey =: 4 : 0
    y =: ('key';x) } y
)

join =: 4 : 0
    if. 2=#x do.
        'key refTable' =. x
        refTable (<key) } y
    end.
    if. 3=#x do.
        'key1 key2 refTable' =. x
        refTable (key1;key2) } y
    end.

)

where =: 4 : 0
    x { y
)

0 [ tbl =: ('a';'b') make_jtable_ (10 2 $ 'aabb');(5+i.10)
0 [ tbl2 =: ('a';'c') make_jtable_ ('aa');('val')

'*' select from tbl
'a' select from tbl
('a';'b') select from tbl
'a' setkey tbl
'*' select ('a';'a';tbl2) join from tbl
'*' select 'a = ''aa'' ' where ('a';'a';tbl2) join from tbl 