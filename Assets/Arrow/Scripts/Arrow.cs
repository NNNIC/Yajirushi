using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ArrowTool;

// 矢印作成
// 取り扱いを簡単にするため１ファイルに集約

[ExecuteInEditMode]
public class Arrow : MonoBehaviour
{
    public enum TYPE
    {
        NONE    =0,

        ONEWAY,

        TURN_L,
        TURN_R,

        U_TURN_L,
        U_TURN_R,

        S_TURN_L,
        S_TURN_R,

        SS_TURN_L,    //Super S -- 大きく曲がったＳ字
        SS_TURN_R,    

        HEAD_C_L,     //先頭がＣの字
        HEAD_C_R,     

        TAIL_C_L,     //末尾がＣの字
        TAIL_C_R,     
    }
    
    [Serializable]
    public class control
    {
        public  Arrow      m_owner; 
        private GameObject m_go    { get { return m_owner.m_go; } }
        private Transform  m_space { get { return m_owner.m_go.transform; } }
        private TYPE       m_type  { get { return m_owner.m_type; } set { m_owner.m_type = value; } }
        private bool       m_isL   { get { return ((int)m_type%2)==0; } }

        #region define bone
        public enum BONE
        {
            ARROW = 0,

            CURVE1,
            CURVE2,
            CURVE3,
            CURVE4,
            CURVE5,

            SHAFT1,
            SHAFT2,
            SHAFT3,
            SHAFT4,
            SHAFT5,

            HEAD,
            ARROW_TOP,

            MID,
            MID1,
            MID2,

            ROOT,

            NUM
        }

        public readonly string[] BNAME = new string[] {

        "arrow",

        "curve1_curve90",
        "curve2_curve90",
        "curve3_curve90",
        "curve4_curve90",
        "curve5_curve90",

        "shaft1_shaft",
        "shaft2_shaft",
        "shaft3_shaft",
        "shaft4_shaft",
        "shaft5_shaft",

        "head",
        "arrow_top",

        "mid",
        "mid1",
        "mid2",

        "root"

        };

        [SerializeField]
        private Transform[] m_bones=null;
        public  Transform GetBone(BONE bone) {
            Transform t = null;
            if (m_bones != null && m_bones.Length == (int)BONE.NUM)
            {
                t = (Transform)m_bones[(int)bone];
                if (t!=null)
                {
                    return t;
                }
            }
            if (m_bones==null || m_bones.Length != (int)BONE.NUM )
            {
                m_bones = new Transform[(int)BONE.NUM];
            }
            t = Util.FindNode(m_go,BNAME[(int)bone]);
            m_bones[(int)bone] = t;
            return t;
        }
        #endregion

        #region node position in the space
        public Vector3 SpaceNodePos(BONE n)
        {
            var target_tr =  GetBone(n);//   Util.FindNode(m_go, n);
            return m_space.InverseTransformPoint(target_tr.position);
        }
        public Vector3 AbsoluteSpacePos(Vector3 v)
        {
            return m_space.TransformPoint(v);
        }
        Vector3 SPOS_ARROW()  { return SpaceNodePos(BONE.ARROW);      }

        Vector3 SPOS_CURVE1() { return SpaceNodePos(BONE.CURVE1);     }
        Vector3 SPOS_CURVE2() { return SpaceNodePos(BONE.CURVE2);     }
        Vector3 SPOS_CURVE3() { return SpaceNodePos(BONE.CURVE3);     }
        Vector3 SPOS_CURVE4() { return SpaceNodePos(BONE.CURVE4);     }
        Vector3 SPOS_CURVE5() { return SpaceNodePos(BONE.CURVE5);     }

        Vector3 SPOS_SHAFT1() { return SpaceNodePos(BONE.SHAFT1);     }
        Vector3 SPOS_SHAFT2() { return SpaceNodePos(BONE.SHAFT2);     }
        Vector3 SPOS_SHAFT3() { return SpaceNodePos(BONE.SHAFT3);     }
        Vector3 SPOS_SHAFT4() { return SpaceNodePos(BONE.SHAFT4);     }
        Vector3 SPOS_SHAFT5() { return SpaceNodePos(BONE.SHAFT5);     }
        #endregion

        #region Hand ITEM
        [Serializable]
        public class handitem
        {
            public int       m_index;
            public Transform m_transform;
            public Vector3   m_space_pos;
            public Vector3   m_save_space_pos;
            public bool      m_bEqual;
            public string    m_name  {get { return m_transform.name;} }
            public Transform m_space;
            public bool      m_ignoreRestore;

            public handitem(Transform space) {m_space = space; }

            public void Reset()
            {
                Update();
                m_bEqual = false;
                m_ignoreRestore  = false;
            }

            public void Update()
            {
                m_space_pos      = m_space.InverseTransformPoint(m_transform.position);
                m_bEqual         = Util.IsEqualVector3(m_save_space_pos,m_space_pos);
                m_save_space_pos = m_space_pos;
                m_ignoreRestore  = false;
            }

            public void Restore()
            {
                if (!m_ignoreRestore) {
                    m_transform.position = m_space.TransformPoint(m_save_space_pos);
                }
            }
        }
        public List<handitem> m_handitems;
        // HandItem用便利関数
        public void HandItem_Add(Transform t) {
            var item = new handitem(m_space);
            item.m_transform = t;
            item.m_index     = m_handitems.Count;
            m_handitems.Add(item);
        }
        public bool HandItem_Update_checkNoChanges()
        {
            if (m_handitems!=null)
            { 
                m_handitems.ForEach(i=>i.Update());
                return m_handitems.TrueForAll(i=>i.m_bEqual==true);
            }
            return true;
        }
        public void  HandItem_Reset()       { m_handitems.ForEach(i=>i.Reset());   }
        public void  HandItem_Restore()     { m_handitems.ForEach(i=>i.Restore()); }
        public float HandItem_SpaceZ(int i) { return m_handitems[i].m_space_pos.z; }
        public float HandItem_SpaceX(int i) { return m_handitems[i].m_space_pos.x; }
        public bool  HandItem_bEqual(int i) { return m_handitems[i].m_bEqual;      }
        public void  HandItem_Ignore(int i) { m_handitems[i].m_ignoreRestore = true; m_handitems[i].m_transform.localPosition = Vector3.zero; }
        #endregion


        #region Move
        public void Move()
        {
            bool bSame = HandItem_Update_checkNoChanges();
            if (bSame) return;

            if (m_handitems != null) {
                if (m_type== TYPE.ONEWAY)
                {
                    move_oneway();
                }
                else if (m_type== TYPE.TURN_R || m_type == TYPE.TURN_L)
                {
                    move_turn();
                }
                else if (m_type== TYPE.U_TURN_R || m_type == TYPE.U_TURN_L )
                {
                    move_u_turn();
                }
                else if (m_type == TYPE.S_TURN_R || m_type == TYPE.S_TURN_L)
                {
                    move_s_turn();
                }
                else if (m_type == TYPE.SS_TURN_R || m_type == TYPE.SS_TURN_L )
                {
                    move_ss_turn();
                }
                else if (m_type == TYPE.HEAD_C_R || m_type == TYPE.HEAD_C_L)
                {
                    move_head_c();
                }
                else if (m_type == TYPE.TAIL_C_R || m_type == TYPE.TAIL_C_L)
                {
                    move_tail_c();
                }

                //絶対値で修正
                HandItem_Restore();
            }
        }

        private void move_oneway()
        {
            var     unit_len   = m_owner.m_unit_len;
            _setZ(BONE.ARROW,HandItem_SpaceZ(0) - unit_len);//Ｚ値のみ反映
        }
        private void move_turn()
        {
            var     unit_len   = m_owner.m_unit_len;
            _setZZ(
                BONE.ARROW, _absP_R(HandItem_SpaceX(0)) - unit_len * 2,  //x値が"arrow"のzに影響 
                BONE.CURVE1,_absP(HandItem_SpaceZ(0)) - unit_len       //z値が"curve1_curve90"に影響
                );
        }
        private void move_u_turn()
        {
            var     unit_len   = m_owner.m_unit_len;
            if (!HandItem_bEqual(0))
            {
                //headのz値  "arrow"の位置に影響
                var arrow_z = SPOS_SHAFT3().z - HandItem_SpaceZ(0) - unit_len;
                _setZZ( 
                    BONE.ARROW , _absP(arrow_z),
                    BONE.CURVE2, _absP_R(HandItem_SpaceX(0) - SPOS_SHAFT2().x) - unit_len
                    );
                if (arrow_z < 0)
                {
                    var curve1_z = SPOS_CURVE1().z + Mathf.Abs(arrow_z);
                    _setZ(BONE.CURVE1,curve1_z);
                }
            }
            else if (!HandItem_bEqual(1))
            {
                var arrow_z  = HandItem_SpaceZ(1) - SPOS_ARROW().z - unit_len;
                var curve1_z = HandItem_SpaceZ(1) - unit_len;
                if (arrow_z >= 0 && curve1_z>=0)
                {
                    _setZZ( //mid0のz値 "arrow"と"curve1_curve90"位置に影響
                        BONE.ARROW , arrow_z,
                        BONE.CURVE1, curve1_z
                        );
                }
            }
            HandItem_Ignore(1);
        }
        private void move_s_turn()
        {
            var     unit_len   = m_owner.m_unit_len;
            if (!HandItem_bEqual(0))
            {
                var hand_z = HandItem_SpaceZ(0);
                var hand_x = HandItem_SpaceX(0);
                var arrow_z = hand_z - SPOS_SHAFT3().z - unit_len;
                _setZZ( 
                    BONE.ARROW,          _absP(arrow_z),
                    BONE.CURVE2, _absP_R(hand_x - SPOS_SHAFT2().x) - unit_len
                    );
                if (arrow_z < 0)
                {
                    var curve1_z = SPOS_CURVE1().z - Mathf.Abs(arrow_z);
                    _setZ(BONE.CURVE1,curve1_z);
                }
            }
            else if (!HandItem_bEqual(1))
            {
                var hand_z = HandItem_SpaceZ(1);

                var arrow_z  = SPOS_ARROW().z - hand_z - unit_len;
                var curve1_z = hand_z - unit_len;
                _setComplexZZ(
                        BONE.ARROW, arrow_z,
                        BONE.CURVE1, curve1_z
                    );
            }
            HandItem_Ignore(1);
        }
        private void move_ss_turn()
        {
            var     unit_len   = m_owner.m_unit_len;
            if (!HandItem_bEqual(0)) //head
            {
                var hand_z = HandItem_SpaceZ(0);
                var hand_x = HandItem_SpaceX(0);

                var arrow_z  = hand_z - SPOS_SHAFT5().z  - unit_len;
                var curve4_z = _lenWoUnit_R(hand_x,SPOS_SHAFT4().x,unit_len);
                _setZZ( BONE.ARROW , _absP(arrow_z),
                        BONE.CURVE4, curve4_z
                        );
                if (arrow_z < 0)
                {
                    _setZ(BONE.CURVE3, SPOS_SHAFT3().z - SPOS_CURVE3().z - arrow_z);
                }
                if (curve4_z < 0)
                {
                    _setZ(BONE.CURVE2, _absP_R(SPOS_CURVE2().x - SPOS_SHAFT2().x) + curve4_z);
                }
            }
            else if (!HandItem_bEqual(1)) //mid
            {
                var hand_x = HandItem_SpaceX(1);
                var curve2_z = _lenWoUnit_R(hand_x , SPOS_SHAFT2().x , unit_len);
                var curve4_z = _lenWoUnit_R(SPOS_CURVE4().x , hand_x , unit_len);

                _setComplexZZ(
                    BONE.CURVE2, curve2_z,
                    BONE.CURVE4, curve4_z
                    );

                var hand_z   = HandItem_SpaceZ(1);
                var curve1_z = hand_z;
                var curve3_z = hand_z - SPOS_CURVE3().z;

                _setComplexZZ(
                        BONE.CURVE1, curve1_z,
                        BONE.CURVE3, curve3_z
                        );

            }
            else if (!HandItem_bEqual(2)) //mid1
            {
                float hand_x = HandItem_SpaceX(2);
                var curve2_z =  _lenWoUnit_R(hand_x , SPOS_SHAFT2().x , unit_len*2);
                var curve4_z =  _lenWoUnit_R(SPOS_CURVE4().x , hand_x , 0);

                _setComplexZZ(
                    BONE.CURVE2, curve2_z,
                    BONE.CURVE4, curve4_z
                    );

                float hand_z  = HandItem_SpaceZ(2);
                var curve3_z = SPOS_SHAFT3().z - hand_z - unit_len;
                var arrow_z  = SPOS_ARROW().z - hand_z - unit_len;

                _setComplexZZ(
                BONE.CURVE3, curve3_z,
                BONE.ARROW , arrow_z
                );
            }
            HandItem_Ignore(1);
            HandItem_Ignore(2);
        }
        private void move_head_c()
        {
            var     unit_len   = m_owner.m_unit_len;
            if (!HandItem_bEqual(0)) //head
            {
                var hand_x = HandItem_SpaceX(0);
                var arrow_z = _lenWoUnit_R(SPOS_SHAFT4().x,hand_x, unit_len);
                _setZ(BONE.ARROW,arrow_z);
                if (arrow_z < 0)
                {
                    var curve2_z = _absP_R(SPOS_CURVE2().x - SPOS_SHAFT2().x) - arrow_z;
                    _setZ(BONE.CURVE2, curve2_z);
                }

                var hand_z = HandItem_SpaceZ(0);
                var curve3_z = hand_z - SPOS_SHAFT3().z - unit_len;
                _setZ(BONE.CURVE3,curve3_z);
                if (curve3_z < 0)
                {
                    var curve1_z = SPOS_CURVE1().z + curve3_z;
                    _setZ(BONE.CURVE1,curve1_z);
                }
            }
            else if (!HandItem_bEqual(1)) //mid
            {
                var hand_x = HandItem_SpaceX(1);
                var curve2_z = _lenWoUnit_R(hand_x,SPOS_SHAFT2().x,unit_len);
                var arrow_z  = _lenWoUnit_R(hand_x,SPOS_ARROW().x,unit_len);
                _setComplexZZ(
                    BONE.CURVE2,curve2_z,
                    BONE.ARROW, arrow_z
                    );
                var hand_z = HandItem_SpaceZ(1);
                var curve3_z = SPOS_CURVE3().z - hand_z;
                var curve1_z = hand_z - unit_len * 2;
                _setComplexZZ(
                    BONE.CURVE3,curve3_z,
                    BONE.CURVE1,curve1_z
                    );
            }
            HandItem_Ignore(1);
        }
        private void move_tail_c()
        {
            var     unit_len   = m_owner.m_unit_len;
            if (!HandItem_bEqual(0)) //head
            {
                var hand_x = HandItem_SpaceX(0);
                var arrow_z = _lenWoUnit_R(hand_x,SPOS_SHAFT4().x,unit_len);
                _setZ(BONE.ARROW,arrow_z);
                if (arrow_z < 0)
                {
                    var curve2_z = _absP_R(SPOS_CURVE2().x - SPOS_SHAFT2().x) + arrow_z;
                    _setZ(BONE.CURVE2, curve2_z);
                }

                var hand_z = HandItem_SpaceZ(0);
                var curve3_z = SPOS_SHAFT3().z - hand_z - unit_len;
                _setZ(BONE.CURVE3, curve3_z);
                if (curve3_z < 0)
                {
                    var curve1_z = SPOS_CURVE1().z - curve3_z;
                    _setZ(BONE.CURVE1, curve1_z);
                }
            }
            else if (!HandItem_bEqual(1)) //mid
            {
                var hand_x = HandItem_SpaceX(1);
                var curve2_z = _lenWoUnit_R(hand_x,SPOS_SHAFT2().x,unit_len);
                var arrow_z  = _lenWoUnit_R(SPOS_ARROW().x,hand_x,unit_len);
                _setComplexZZ(
                    BONE.CURVE2,curve2_z,
                    BONE.ARROW, arrow_z
                    );
                var hand_z = HandItem_SpaceZ(1);
                var curve3_z = hand_z - SPOS_CURVE3().z;
                var curve1_z = hand_z;
                _setComplexZZ(
                    BONE.CURVE3,curve3_z,
                    BONE.CURVE1,curve1_z
                    );
            }


            HandItem_Ignore(1);
            
        }

        private void _setZ(BONE n, float z)
        {
            _setLocalClampZ(n,z);
        }
        private void _setZZ(BONE n1, float z1, BONE n2, float z2)
        {
            _setLocalClampZ(n1,z1);
            _setLocalClampZ(n2,z2);
        }
        private void _setComplexZZ(BONE n1, float z1, BONE n2, float z2)
        {
            if (z1>=0 && z2>=0)
            {
                _setLocalClampZ(n1,z1);
                _setLocalClampZ(n2,z2);
            }
        }
        private void  _setLocalClampZ(BONE n, float z)     {
            var t = GetBone(n); 
            t.localPosition = Util.Vector3_ModZ(t.localPosition,_clamp(z));
        }

        private float _clamp(float a)                        { return Mathf.Clamp(a,0,float.MaxValue);                  }
        private float _absP_R(float a) // タイプが"XXX_R"時にプラスを期待し、"XXX_L"時はマイナスとなる
        {
            return m_isL ? _absM(a) : _absP(a);
        }
        private float _absM_R(float a)
        {
            return m_isL ? _absP(a) : _absM(a);
        }
        private float _absP(float a)  { return a>0 ? a : 0; }
        private float _absM(float a) { return a<0 ? -a: 0; }
        private float _lenWoUnit_R(float a, float b, float unit) //右時 a-b-unit 左時 b-a-unitを返す
        {
            return m_isL ? b - a - unit : a - b - unit;
        }

        #endregion
    }

    public float    m_shuft_width    = 0.4f;
    public readonly float m_unit_len = 1.0f; //１以外は保証外
    public float    m_scale          = 1;
    public int      m_curve_divnum   = 10;
    public float    m_arrow_width    = 1.3f;

    [HideInInspector]
    public TYPE __type;
    public  TYPE  m_type;

    [HideInInspector]
    public GameObject m_go;

    [HideInInspector]
    public control m_control;

    public Transform GetHandle(int i)
    {
        if (m_control!=null && m_control.m_handitems!=null)
        {
            if (i>=0 && i<m_control.m_handitems.Count)
            {
                return m_control.m_handitems[i].m_transform;
            }
        }
        return null;
    }
    public int SizeHandle()
    {
        if (m_control!=null && m_control.m_handitems!=null)
        {
            return m_control.m_handitems.Count;
        }
        return 0;
    }

    #region 主にEDITOR用
    Transform m_head;
    public Transform GetHead()
    {
        if (m_go==null) return null;
        if (m_head==null)
        {
            m_head = Util.FindNode(m_go,"head");
        }
        return m_head;
    }
    Transform m_arrow_top;
    public Transform GetArrowTop()
    {
        if (m_go==null) return null;
        if (m_arrow_top==null)
        {
            m_arrow_top = Util.FindNode(m_go,"arrow_top");
        }
        return m_arrow_top;
    }
    Transform m_mid;
    public Transform GetMid()
    {
        if (m_go==null) return null;
        if (m_mid==null)
        {
            m_mid = Util.FindNode(m_go,"mid");
        }
        return m_mid;
    }
    public void Adjust()
    {
        if (m_control==null) return;
        switch(m_type) {
            case TYPE.S_TURN_R:
            case TYPE.S_TURN_L:
                {
                    var arrow_pos = m_control.SpaceNodePos(control.BONE.ARROW);
                    var root_pos  = m_control.SpaceNodePos(control.BONE.ROOT);
                    var mid_pos   = m_control.SpaceNodePos(control.BONE.MID);

                    var next_mid_pos = Util.Vector3_ModZ(mid_pos,(arrow_pos.z + root_pos.z) * 0.5f);

                    var mid =  m_control.GetBone(control.BONE.MID);
                    mid.position = m_control.AbsoluteSpacePos(next_mid_pos);
                }
                break;

            case TYPE.SS_TURN_R:
            case TYPE.SS_TURN_L:
                {
                    var arrow_pos = m_control.SpaceNodePos(control.BONE.ARROW);
                    var root_pos  = m_control.SpaceNodePos(control.BONE.ROOT);

                    var next_mid1_pos = m_control.AbsoluteSpacePos((arrow_pos + root_pos)* 0.5f);

                    var mid1 =  m_control.GetBone(control.BONE.MID1);
                    mid1.position = m_control.AbsoluteSpacePos(next_mid1_pos);
                }
                break;
        }

    }
    #endregion

    public void Update()
    {
        if (__type != m_type)
        {
            m_go = null;
            m_control = null;
            if (transform.childCount>0)
            {
                GameObject.DestroyImmediate(transform.GetChild(0).gameObject);
            }
            if (transform.childCount==0)
            {
                __type = m_type;
            }
            else
            { 
                return;
            }
        }

        if (m_type!= TYPE.NONE && m_go==null)
        {
            m_go = ArrowMaker.CreateArrowSub(__type,m_shuft_width,m_unit_len,m_curve_divnum,m_arrow_width);
            m_go.transform.parent = transform;
            transform.localEulerAngles      = Vector3.zero;
            m_go.transform.localEulerAngles = Vector3.zero;
            m_go.transform.localScale       = m_scale * Vector3.one;
            m_go.transform.localPosition    = Vector3.zero;

            m_control = new control();
            m_control.m_owner = this;

            var headbone  = Util.FindNode(m_go,"head");

            var midbone_0 = Util.FindNode(m_go,"mid");
            var midbone_1 = Util.FindNode(m_go,"mid1");
            var midbone_2 = Util.FindNode(m_go,"mid2");

            m_control.m_handitems = new List<control.handitem>();

            m_control.HandItem_Add(headbone);

            if (midbone_0!=null) m_control.HandItem_Add(midbone_0);
            if (midbone_1!=null) m_control.HandItem_Add(midbone_1);
            if (midbone_2!=null) m_control.HandItem_Add(midbone_2);
        }
        
        if (m_control!=null)
        {
            m_control.Move();
        }
    }
}

// ------------------------------------------------ 以下、矢印作成 --------------------------------------------

namespace ArrowTool
{
    public class Util
    {
        public static bool IsEqualVector3(Vector3 v1, Vector3 v2)
        {
            var dv = v1-v2;
            float epsiron = 0.001f;  //floatの有効桁は6桁
            var dx = Mathf.Abs(dv.x);
            var dy = Mathf.Abs(dv.y);
            var dz = Mathf.Abs(dv.z);

            return ( (dx<epsiron) && (dy<epsiron) && (dz<epsiron) );
        }

        public static Transform FindNode(GameObject go, string name)
        {
            Transform found = null;
            Action<Transform> _traverse = null;
            _traverse = (t)=> {
                if (found==null)
                {
                    if (t.name==name)
                    {
                        found = t;
                        return;
                    }

                    for(int i = 0; i<t.childCount; i++)
                    {
                        _traverse(t.GetChild(i));
                    }
                }
            };

            _traverse(go.transform);

            return found;
        }

        public static Vector3 Vector3_ModZ(Vector3 v, float z)
        {
            return new Vector3(v.x,v.y,z);
        }

        public static Vector3 Vector3_AddZ(Vector3 v, float z)
        {
            return v + Vector3.forward * z;
        }
    }
}


public class ArrowMaker {
    //
    // 方針：頂点などを記号化した状態のままのデータ構造として、扱いやすい形で
    //       
    //       最終的にスキンメッシュを作成する。
    //

#region 格納クラス
    //
    public class vertex
    {
#region 名前
        private List<string> names = new List<string>();
        public string name {
            get {  return names.Count>0 ? names[names.Count-1] : null; } // 最後に登録したものを返す
            set {  if (!names.Contains(value)) { names.Add(value); }   } // ユニークのみ
        }
        public bool   name_contains(string s) { return names.Contains(s);   }
        public bool   names_have_startsWith(string s)
        {
            foreach(var n in names)
            {
                if (n.StartsWith(s)) return true;
            }
            return false;
        }
#endregion

        public Vector3    v;       //座標
        public string     bone0;   //所属ボーン0
        public string     bone1;   //所属ボーン1

        public float      weight0; //ウエイト0
        public float      weight1; //ウエイト1

        public int        tmp_index; //メッシュ作成時に使用の一時的インデックス

        public vertex(string i_name, Vector3 vec3)
        {
            _init(i_name,vec3);
        }
        public vertex(string i_name,  float x, float y, float z)
        {
            _init(i_name, new Vector3(x,y,z));
        }
        private void _init(string i_name, Vector3 vec3)
        {
            name = i_name;

            v = vec3;

            bone0 = null; //未定義
            bone1 = null; //未定義
            weight0 = 1;
            weight1 = 0;
        }

    }

    public class poligon
    {
        public List<vertex> vertex_list   = new List<vertex>();    //頂点リスト

        public struct trindex {public Vector3 idx0, idx1, idx2; }                            //３角形情報を頂点で管理（※インデックスではない）
        public List<trindex>  tri_index_list= new List<trindex>();                           //トライアングルインデックス

#region 利用時に便宜
        private string  _tmpsaved_poligonname;                                               //利用時に便宜
        public void     Begin(string poligonname)
        {
            _tmpsaved_poligonname = poligonname;
        }
        public void     End()
        {
            _tmpsaved_poligonname = null;
        }
#endregion

        private string _makename(string vertexname) { return _tmpsaved_poligonname + ">" + vertexname;  }
        private string _makename(string poligonname,string vertexname) { return poligonname + ">" + vertexname;  }

        public void AddVertex(string vertexname, float x, float y, float z) {
            AddVertex(vertexname, new Vector3(x,y,z));
        }
        public void AddVertex(string vertexname, Vector3 vec3)
        {
            if (_tmpsaved_poligonname==null) throw new SystemException();
            bool bFound = false;
            for(var i =0; i<vertex_list.Count; i++)
            {
                var v = vertex_list[i];
                var b = Util.IsEqualVector3(v.v, vec3);
                if (b)
                {
                    v.name = _makename(vertexname); //名前を変える　（内部にてhistory保存）
                    bFound = true;
                    break;
                }
            }
            if (!bFound)
            {
                vertex_list.Add(new vertex(_makename(vertexname), vec3));
            }
        }

        public void AddTriIndex(string n1, string n2, string n3)
        {
            tri_index_list.Add(new trindex() { idx0=Pos(n1), idx1=Pos(n2), idx2=Pos(n3) });
        }

        //検索
        public vertex FindVertex(string vertexname)
        {
            var key = _makename(vertexname);
            foreach(var v in vertex_list)
            {
                if (v.name_contains(key))
                {
                    return v;
                }
            }
            return null;
        }
        public vertex FindVertex(Vector3 pos)
        {
            foreach(var v in vertex_list)
            {
                if (Util.IsEqualVector3 (v.v, pos)) return v;
            }
            return null;
        }
        private Vector3 Pos(string vertexname)
        {
            var v = FindVertex(vertexname);
            if (v==null)
            {
                throw new SystemException();
            }
            return v.v;
        }
        public List<vertex> CollectVertex(string poligonname,bool bLatestOnly=true)
        {
            var list = new List<vertex>();
            var nullkeyname = _makename(poligonname,"");
            foreach(var v in vertex_list)
            {
                bool b=false;
                if (bLatestOnly)
                {
                    b = v.name.StartsWith(nullkeyname);
                }
                else
                {
                    b = v.names_have_startsWith(nullkeyname);
                }
                if (b)
                {
                    list.Add(v);
                }
            }
            return list;
        }


        //メッシュ作成用
        public void MESH_create(
                                Skin skin,
                                ref List<Vector3>    mesh_vertex_list, 
                                ref List<int>        mesh_triangle_list, 
                                ref List<Vector2>    mesh_uvlist,
                                ref List<BoneWeight> mesh_weight_list
        )
        {
            var index = mesh_vertex_list.Count;
            var tmp_list = new List<vertex>();
            foreach(var vtx in vertex_list)
            {
                vtx.tmp_index = index++;
                tmp_list.Add(vtx); 
                mesh_vertex_list.Add(vtx.v); //頂点
                mesh_uvlist.Add(Vector2.zero);   //UV暫定

                //BoneWeight
                BoneWeight bw = new BoneWeight();
                bw.boneIndex0 = skin.FindBoneIndex(vtx.bone0);
                if (vtx.bone1!=null)
                {
                    bw.boneIndex1 = skin.FindBoneIndex(vtx.bone1);
                }
                bw.weight0 = vtx.weight0;
                bw.weight1 = vtx.weight1;

                mesh_weight_list.Add(bw);
            }

            Func<Vector3,int> get_index = (v)=> {
                var vrtx = FindVertex(v);
                return vrtx.tmp_index;
            };
            foreach(var t in tri_index_list)
            {
                mesh_triangle_list.Add(get_index(t.idx0)); // 三角形インデックス
                mesh_triangle_list.Add(get_index(t.idx1)); //
                mesh_triangle_list.Add(get_index(t.idx2)); //
            }
        }
    }

    public class bone
    {
        public string     name;

        public string     parent;                    //親ボーン名

        public Vector3    org;                       //起点
        public float      angle_y;                   //方向

        public Transform  tmp_tr;                    //メッシュ作成時に利用

        public bone(string i_name, string i_panrent, Vector3 i_org, float i_angle_y)
        {
            name     = i_name;
            parent   = i_panrent;
            org      = i_org;
            angle_y  = i_angle_y;
        }
    }

    public class Skin
    {
        public poligon       pol       = new poligon();
        public List<bone>    bone_list = new List<bone>();

        public float   shaft_width;   
        public float   next_angle_y;  //次のY軸角
        public Vector3 next_org;      //次の起点

        public void AddBone(string name, Vector3 org, float angle_y)
        {
            string parent = null; //本パターンでは必ず１つ前が親
            if (bone_list.Count>0)
            {
                parent = bone_list[bone_list.Count-1].name;
            }
            var bone = new bone(name,parent,org,angle_y);
            bone_list.Add(bone);
        }

        public void InsertMidBone(string parent, string index=null) //ハンドルとなる中間点
        {
            var parent_bone = bone_list.Find(i=>i.name==parent);

            var bone = new bone("mid" + index,parent,parent_bone.org, parent_bone.angle_y);
            bone_list.Add(bone);
        }

        public void SetWeight(string poligon, string bone0)
        {
            var vertex_list = pol.CollectVertex(poligon);
            foreach(var v in vertex_list)
            {
                v.bone0 = bone0;
            }
        }
        //検索
        public int FindBoneIndex(string name)
        {
            var idx =  bone_list.FindIndex(i=>i.name == name);
            if (idx<0)
                throw new SystemException();
            return idx;
        }
        public bone FindBone(string name)
        {
            return bone_list.Find(i=>i.name == name);
        }

        public void SKINNEDMESH_createBones(Transform transform,  out Transform[] bones, out Matrix4x4[] bindPoses)
        {
            var tf_list = new List<Transform>();
            var bp_list = new List<Matrix4x4>();

            foreach(var b in bone_list)
            {
                var bp   = Matrix4x4.identity;
                var go   = new GameObject(b.name);
                var tr   = go.transform;
                tr.rotation = Quaternion.Euler(0,b.angle_y,0);
                tr.position = b.org;

                b.tmp_tr = tr;
                tr.parent = (b.parent!=null) ?  FindBone(b.parent).tmp_tr : transform;

                //tr.localPosition = b.org - tr.parent.position;
                bp = tr.worldToLocalMatrix * transform.localToWorldMatrix;
                
                tf_list.Add(tr);
                bp_list.Add(bp);
            }

            bones     = tf_list.ToArray();
            bindPoses = bp_list.ToArray();
        }
    }
#endregion

#region ポリゴン作成
    // 矢印の種類
    //
    //  一方向
    //  直角―右・左       
    //  Uターンー右・左
    //  Ｓターンー右・左
    //  

    /// <summary>
    ///      
    ///  長方形を作成
    ///  　　oが底辺の中央　Zポジションを指定
    /// 
    ///          width
    ///      |<------->| c
    ///    b +---------+ ^ 
    ///      |         | |len
    ///      |         | |
    ///    a |----o----| v d    
    ///      
    /// </summary>
    private poligon POL_CreateRectangle(poligon pol, string poligon_name, float width, float len, Vector3 org, float angle_y)
    {
        pol.Begin(poligon_name);
        {
            var a = "a";
            var b = "b";
            var c = "c";
            var d = "d";

            var wh = width / 2.0f;

            var va = new Vector3(-wh,0,0);
            var vb = new Vector3(-wh,0,len);
            var vc = new Vector3( wh,0,len);
            var vd = new Vector3( wh,0,0);

            var mat = Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(0,angle_y,0),Vector3.one);

            Func<Vector3,Vector3> _rot = (v)=> {
                return (Vector3)(mat *v) + org;
            };

            va = _rot(va);
            vb = _rot(vb);
            vc = _rot(vc);
            vd = _rot(vd);

            pol.AddVertex(a,va);
            pol.AddVertex(b,vb);
            pol.AddVertex(c,vc);
            pol.AddVertex(d,vd);

            pol.AddTriIndex(a, b, c);
            pol.AddTriIndex(c, d, a);

        }
        pol.End();
        return pol;
    }

    /// <summary>
    ///   90度のカーブの曲線
    ///       ________ an
    ///      /     　　^ outer radius
    ///     /        bn|
    ///    /     /~~~~ | ^inner radius
    ///   /     /      | 
    /// a0|  o  |b0    |
    ///      <-------->c
    ///         radius
    /// </summary>
    private poligon POL_CreateRightCurve(poligon pol, string poligon_name, float width, float radius, float angle_y, Vector3 org,int divnum)
    {
        //　１． oを0,0,0とする
        //  ２． 右曲がりで９０度の半円を外側と内側に作る .. bReverse自は逆
        //  ３． angle_yで傾ける
        //  ４． orgへ移動

        pol.Begin(poligon_name);
        {
            var wh = width / 2.0f;
            var circle_cneter = Vector3.right * radius;
            var outer_radius  = radius + wh;
            var inner_radius  = radius - wh;

            var step = 90.0f / divnum;
            var list_a = new List<Vector3>();
            var list_b = new List<Vector3>();
            for(var i = 0; i< divnum+1; i++)
            {
                var dx = Mathf.Cos(i * step * Mathf.Deg2Rad);
                var dz = Mathf.Sin(i * step * Mathf.Deg2Rad);
                var dv = Vector3.left * dx + Vector3.forward * dz;
                list_a.Add( outer_radius * dv + circle_cneter);
                list_b.Add( inner_radius * dv + circle_cneter);
            }

            var mat = Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(0,angle_y,0),Vector3.one);

            Func<Vector3,Vector3> _rot = (v)=> {
                return (Vector3)(mat *v) + org;
            };
        
            for(var i=0; i<list_a.Count; i++)
            {
                var av = _rot(list_a[i]);
                var bv = _rot(list_b[i]);

                Func<int, string> a = (n) => ("a" + n.ToString());
                Func<int, string> b = (n) => ("b" + n.ToString());

                pol.AddVertex(a(i), av); // an
                pol.AddVertex(b(i), bv); // bn

                if (i > 0)
                {
                    var prev = i - 1;
                    var a0 = a(prev);
                    var b0 = b(prev);
                    var a1 = a(i);
                    var b1 = b(i);

                    pol.AddTriIndex(a0, a1, b1);
                    pol.AddTriIndex(b1, b0, a0);
                }
            }
        }
        pol.End();

        return pol;
    }
    private poligon POL_CreateLeftCurve(poligon pol, string poligon_name, float width, float radius, float angle_y, Vector3 org,int divnum)
    {
        //　１． oを0,0,0とする
        //  ２． 右曲がりで９０度の半円を外側と内側に作る .. bReverse自は逆
        //  ３． angle_yで傾ける
        //  ４． orgへ移動

        pol.Begin(poligon_name);
        {
            var wh = width / 2.0f;
            var circle_cneter = Vector3.left * radius;
            var outer_radius  = radius + wh;
            var inner_radius  = radius - wh;

            var step = 90.0f / divnum;
            var list_a = new List<Vector3>();
            var list_b = new List<Vector3>();
            for(var i = 0; i< divnum+1; i++)
            {
                var dx = Mathf.Cos(i * step * Mathf.Deg2Rad);
                var dz = Mathf.Sin(i * step * Mathf.Deg2Rad);
                var dv = Vector3.right * dx + Vector3.forward * dz;
                list_a.Add( inner_radius * dv + circle_cneter);
                list_b.Add( outer_radius * dv + circle_cneter);
            }

            var mat = Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(0,angle_y,0),Vector3.one);

            Func<Vector3,Vector3> _rot = (v)=> {
                return (Vector3)(mat *v) + org;
            };
        
            for(var i=0; i<list_a.Count; i++)
            {
                var av = _rot(list_a[i]);
                var bv = _rot(list_b[i]);

                Func<int, string> a = (n) => ("a" + n.ToString());
                Func<int, string> b = (n) => ("b" + n.ToString());

                pol.AddVertex(a(i), av); // an
                pol.AddVertex(b(i), bv); // bn

                if (i > 0)
                {
                    var prev = i - 1;
                    var a0 = a(prev);
                    var b0 = b(prev);
                    var a1 = a(i);
                    var b1 = b(i);

                    pol.AddTriIndex(a0, a1, b1);
                    pol.AddTriIndex(b1, b0, a0);
                }
            }
        }
        pol.End();

        return pol;
    }

    /// <summary>
    ///              c         
    ///              /⧵
    ///             /  ⧵
    ///            /    ⧵
    ///           /      ⧵
    ///          / a o  e ⧵ 
    ///        b --+-()-+-- d
    ///            |    |   
    /// 
    /// </summary>
    private poligon POL_CreateArrowRoot(poligon pol, string poligon_name, float width, float height, float shaftwidth, float angle_y, Vector3 org )
    {
        pol.Begin(poligon_name);
        {
            var a = "a";
            var b = "b";
            var c = "c";
            var d = "d";
            var e = "e";

            var ahw = width / 2.0f;
            var shw = shaftwidth / 2.0f;

            var va = new Vector3(-shw,0,0     );
            var vb = new Vector3(-ahw,0,0     );
            var vc = new Vector3(   0,0,height);
            var vd = new Vector3( ahw,0,0     );
            var ve = new Vector3( shw,0,0     );

            var mat = Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(0,angle_y,0),Vector3.one);

            Func<Vector3,Vector3> _rot = (v)=> {
                return (Vector3)(mat *v) + org;
            };

            va = _rot(va);
            vb = _rot(vb);
            vc = _rot(vc);
            vd = _rot(vd);
            ve = _rot(ve);

            pol.AddVertex(a,va);           //a
            pol.AddVertex(b,vb);           //b
            pol.AddVertex(c,vc);           //c
            pol.AddVertex(d,vd);           //d
            pol.AddVertex(e,ve);           //e

            //a-b-c, a-c-e, e-c-d
            pol.AddTriIndex(a,b,c);
            pol.AddTriIndex(a,c,e);
            pol.AddTriIndex(e,c,d);

        }
        pol.End();
        return pol;
    }
#endregion

#region スキンパーツ作成用
    private Skin SKINPARTS_createRoot(float width) 
    {
        var skin = new Skin();

        skin.AddBone("root",Vector3.zero,0);

        skin.shaft_width = width;
        skin.next_angle_y = 0;
        skin.next_org = Vector3.zero;

        return skin;
    }
    private Skin SKINPARTS_addShaft(Skin skin, string id, float len)
    {
        var name    = id + "_shaft";
        var width   = skin.shaft_width;
        var org     = skin.next_org;
        var angle_y = skin.next_angle_y;

        POL_CreateRectangle(skin.pol, name,width,len, org,angle_y);

        skin.AddBone(name,org,angle_y);

        skin.SetWeight(name,name);

        skin.next_org = org + (Vector3)(Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(0,angle_y,0),Vector3.one) * Vector3.forward);

        return skin;
    }
    private Skin SKINPARTS_addCurve90(Skin skin, string id, float radius, int divnum, bool bRev)
    {
        var name    = id + "_curve90";
        var width   = skin.shaft_width;
        var org     = skin.next_org;
        var angle_y = skin.next_angle_y;

        if (bRev)
        {
            POL_CreateLeftCurve(skin.pol,name,width,radius,angle_y,org,divnum);
            skin.AddBone(name,org,angle_y);
            skin.SetWeight(name,name);
            skin.next_org = org + (Vector3)(Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(0,angle_y,0),Vector3.one) * (new Vector3(-radius,0,radius)));
            skin.next_angle_y -= 90;
        }
        else
        {
            POL_CreateRightCurve(skin.pol,name,width,radius,angle_y,org,divnum);
            skin.AddBone(name,org,angle_y);
            skin.SetWeight(name,name);
            skin.next_org = org + (Vector3)(Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(0,angle_y,0),Vector3.one) * (new Vector3( radius,0,radius)));
            skin.next_angle_y += 90;
        }
        
        return skin;
    }  
    private Skin SKINPARTS_addArrowRoot(Skin skin, float width, float len)
    {
        var name       = "arrow";
        var shaftwidth = skin.shaft_width;
        var org        = skin.next_org;
        var angle_y    = skin.next_angle_y;

        POL_CreateArrowRoot(skin.pol, name,width,len,shaftwidth,angle_y,org);

        skin.AddBone(name, org, angle_y);

        skin.SetWeight(name,name);
        
        skin.next_org += (Vector3)(Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(0,angle_y,0),Vector3.one) * Vector3.forward);

        return skin;
    }
    private Skin SKINPARTS_addArrowTop(Skin skin)
    {
        var name       = "arrow_top";
        var org        = skin.next_org;
        var angle_y    = skin.next_angle_y;

        skin.AddBone(name, org, angle_y);

        skin.next_org = org;//(Vector3)(Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(0,angle_y,0),Vector3.one) * Vector3.forward);

        return skin;
    }
    private Skin SKINPARTS_addArrowHead(Skin skin)
    {
        var name       = "head";
        var org        = skin.next_org;
        var angle_y    = skin.next_angle_y;

        skin.AddBone(name, org, angle_y);

        skin.next_org = (Vector3)(Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(0,angle_y,0),Vector3.one) * Vector3.forward);

        return skin;
    }
#endregion

#region スキン作成
    //一方向矢印のスキン作成
    private Skin CreateSkin_OneWay_Arrow(float shaft_width, float unit_len, int unit_divnum, float arrow_width)
    {
        var skin = SKINPARTS_createRoot(shaft_width);

        skin = SKINPARTS_addShaft(skin,"shaft",unit_len);

        skin = SKINPARTS_addArrowRoot(skin,arrow_width,unit_len);

        skin = SKINPARTS_addArrowTop(skin);

        skin = SKINPARTS_addArrowHead(skin);

        return skin;
    }

    //右方向矢印のスキン作成
    private Skin CreateSkin_Curve_R_Arrow(float shaft_width, float unit_len, int unit_divnum, float arrow_width)
    {
        var skin = SKINPARTS_createRoot(shaft_width);

        skin = SKINPARTS_addShaft(skin,"shaft1",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve1",unit_len,unit_divnum,false);

        skin = SKINPARTS_addShaft(skin,"shaft2",unit_len);

        skin = SKINPARTS_addArrowRoot(skin,arrow_width,unit_len);

        skin = SKINPARTS_addArrowTop(skin);

        skin = SKINPARTS_addArrowHead(skin);

        return skin;
    }
    //左方向矢印のスキン作成
    private Skin CreateSkin_Curve_L_Arrow(float shaft_width, float unit_len, int unit_divnum, float arrow_width)
    {
        var skin = SKINPARTS_createRoot(shaft_width);

        skin = SKINPARTS_addShaft(skin,"shaft1",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve1",unit_len,unit_divnum,true);

        skin = SKINPARTS_addShaft(skin,"shaft2",unit_len);

        skin = SKINPARTS_addArrowRoot(skin,arrow_width,unit_len);

        skin = SKINPARTS_addArrowTop(skin);

        skin = SKINPARTS_addArrowHead(skin);

        return skin;
    }
    //Ｕターン右用のスキン作成
    private Skin CreateSkin_U_Turn_R_Arrow(float shaft_width, float unit_len, int unit_divnum, float arrow_width)
    {
        var skin = SKINPARTS_createRoot(shaft_width);

        skin = SKINPARTS_addShaft(skin,"shaft1",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve1",unit_len,unit_divnum,false);

        skin = SKINPARTS_addShaft(skin,"shaft2",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve2",unit_len,unit_divnum,false);

        skin = SKINPARTS_addShaft(skin,"shaft3",unit_len);

        skin = SKINPARTS_addArrowRoot(skin,arrow_width,unit_len);

        skin = SKINPARTS_addArrowTop(skin);

        skin = SKINPARTS_addArrowHead(skin);

        skin.InsertMidBone("shaft2_shaft");
        

        return skin;
    }
    //Uターン左用のスキン作成
    private Skin CreateSkin_U_Turn_L_Arrow(float shaft_width, float unit_len, int unit_divnum, float arrow_width)
    {
        var skin = SKINPARTS_createRoot(shaft_width);

        skin = SKINPARTS_addShaft(skin,"shaft1",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve1",unit_len,unit_divnum,true);

        skin = SKINPARTS_addShaft(skin,"shaft2",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve2",unit_len,unit_divnum,true);

        skin = SKINPARTS_addShaft(skin,"shaft3",unit_len);

        skin = SKINPARTS_addArrowRoot(skin,arrow_width,unit_len);

        skin = SKINPARTS_addArrowTop(skin);

        skin = SKINPARTS_addArrowHead(skin);

        skin.InsertMidBone("shaft2_shaft");

        return skin;
    }
    //Sターン右用のスキン作成
    private Skin CreateSkin_S_Turn_R_Arrow(float shaft_width, float unit_len, int unit_divnum, float arrow_width)
    {
        var skin = SKINPARTS_createRoot(shaft_width);

        skin = SKINPARTS_addShaft(skin,"shaft1",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve1",unit_len,unit_divnum,false);

        skin = SKINPARTS_addShaft(skin,"shaft2",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve2",unit_len,unit_divnum,true);

        skin = SKINPARTS_addShaft(skin,"shaft3",unit_len);

        skin = SKINPARTS_addArrowRoot(skin,arrow_width,unit_len);

        skin = SKINPARTS_addArrowTop(skin);

        skin = SKINPARTS_addArrowHead(skin);

        skin.InsertMidBone("shaft2_shaft");

        return skin;
    }
    //Sターン左用のスキン作成
    private Skin CreateSkin_S_Turn_L_Arrow(float shaft_width, float unit_len, int unit_divnum, float arrow_width)
    {
        var skin = SKINPARTS_createRoot(shaft_width);

        skin = SKINPARTS_addShaft(skin,"shaft1",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve1",unit_len,unit_divnum,true);

        skin = SKINPARTS_addShaft(skin,"shaft2",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve2",unit_len,unit_divnum,false);

        skin = SKINPARTS_addShaft(skin,"shaft3",unit_len);

        skin = SKINPARTS_addArrowRoot(skin,arrow_width,unit_len);

        skin = SKINPARTS_addArrowTop(skin);

        skin = SKINPARTS_addArrowHead(skin);

        skin.InsertMidBone("shaft2_shaft");

        return skin;
    }
    //SSターン右用のスキン作成
    //
    //
    //    +---+  ^
    //    |   |  | 
    //        +---
    private Skin CreateSkin_SS_Turn_R_Arrow(float shaft_width, float unit_len, int unit_divnum, float arrow_width)
    {
        var skin = SKINPARTS_createRoot(shaft_width);

        skin = SKINPARTS_addShaft(skin,"shaft1",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve1",unit_len,unit_divnum,false);

        skin = SKINPARTS_addShaft(skin,"shaft2",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve2",unit_len,unit_divnum,false);

        skin = SKINPARTS_addShaft(skin,"shaft3",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve3",unit_len,unit_divnum,true);

        skin = SKINPARTS_addShaft(skin,"shaft4",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve4",unit_len,unit_divnum,true);

        skin = SKINPARTS_addShaft(skin,"shaft5",unit_len);

        skin = SKINPARTS_addArrowRoot(skin,arrow_width,unit_len);

        skin = SKINPARTS_addArrowTop(skin);

        skin = SKINPARTS_addArrowHead(skin);

        //skin.InsertMidBone("shaft2_shaft");
        skin.InsertMidBone("shaft3_shaft");
        skin.InsertMidBone("shaft4_shaft","1");
        
        return skin;
    }
    private Skin CreateSkin_SS_Turn_L_Arrow(float shaft_width, float unit_len, int unit_divnum, float arrow_width)
    {
        var skin = SKINPARTS_createRoot(shaft_width);

        skin = SKINPARTS_addShaft(skin,"shaft1",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve1",unit_len,unit_divnum,true);

        skin = SKINPARTS_addShaft(skin,"shaft2",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve2",unit_len,unit_divnum,true);

        skin = SKINPARTS_addShaft(skin,"shaft3",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve3",unit_len,unit_divnum,false);

        skin = SKINPARTS_addShaft(skin,"shaft4",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve4",unit_len,unit_divnum,false);

        skin = SKINPARTS_addShaft(skin,"shaft5",unit_len);

        skin = SKINPARTS_addArrowRoot(skin,arrow_width,unit_len);

        skin = SKINPARTS_addArrowTop(skin);

        skin = SKINPARTS_addArrowHead(skin);

        //skin.InsertMidBone("shaft2_shaft");
        skin.InsertMidBone("shaft3_shaft");
        skin.InsertMidBone("shaft4_shaft","1");
        
        return skin;
    }
    // 先頭部分がクランプ
    //             <---+
    //                 |
    //             +---+
    //             |
    private Skin CreateSkin_HEAD_C_R_Arrow(float shaft_width, float unit_len, int unit_divnum, float arrow_width)
    {
        var skin = SKINPARTS_createRoot(shaft_width);

        skin = SKINPARTS_addShaft(skin,"shaft1",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve1",unit_len,unit_divnum,false);

        skin = SKINPARTS_addShaft(skin,"shaft2",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve2",unit_len,unit_divnum,true);

        skin = SKINPARTS_addShaft(skin,"shaft3",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve3",unit_len,unit_divnum,true);

        skin = SKINPARTS_addShaft(skin,"shaft4",unit_len);

        skin = SKINPARTS_addArrowRoot(skin,arrow_width,unit_len);

        skin = SKINPARTS_addArrowTop(skin);

        skin = SKINPARTS_addArrowHead(skin);

        //skin.InsertMidBone("shaft2_shaft");
        skin.InsertMidBone("shaft3_shaft");

        return skin;
    }
    private Skin CreateSkin_HEAD_C_L_Arrow(float shaft_width, float unit_len, int unit_divnum, float arrow_width)
    {
        var skin = SKINPARTS_createRoot(shaft_width);

        skin = SKINPARTS_addShaft(skin,"shaft1",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve1",unit_len,unit_divnum,true);

        skin = SKINPARTS_addShaft(skin,"shaft2",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve2",unit_len,unit_divnum,false);

        skin = SKINPARTS_addShaft(skin,"shaft3",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve3",unit_len,unit_divnum,false);

        skin = SKINPARTS_addShaft(skin,"shaft4",unit_len);

        skin = SKINPARTS_addArrowRoot(skin,arrow_width,unit_len);

        skin = SKINPARTS_addArrowTop(skin);

        skin = SKINPARTS_addArrowHead(skin);

        //skin.InsertMidBone("shaft2_shaft");
        skin.InsertMidBone("shaft3_shaft");

        return skin;
    }
    //根本がクランプ
    //
    //       +---+   
    //       |   |   
    //           +--->
    private Skin CreateSkin_TAIL_C_R_Arrow(float shaft_width, float unit_len, int unit_divnum, float arrow_width)
    {
        var skin = SKINPARTS_createRoot(shaft_width);

        skin = SKINPARTS_addShaft(skin,"shaft1",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve1",unit_len,unit_divnum,false);

        skin = SKINPARTS_addShaft(skin,"shaft2",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve2",unit_len,unit_divnum,false);

        skin = SKINPARTS_addShaft(skin,"shaft3",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve3",unit_len,unit_divnum,true);

        skin = SKINPARTS_addShaft(skin,"shaft4",unit_len);

        skin = SKINPARTS_addArrowRoot(skin,arrow_width,unit_len);

        skin = SKINPARTS_addArrowTop(skin);

        skin = SKINPARTS_addArrowHead(skin);

        //skin.InsertMidBone("shaft2_shaft");
        skin.InsertMidBone("shaft3_shaft");

        return skin;
    }
    private Skin CreateSkin_TAIL_C_L_Arrow(float shaft_width, float unit_len, int unit_divnum, float arrow_width)
    {
        var skin = SKINPARTS_createRoot(shaft_width);

        skin = SKINPARTS_addShaft(skin,"shaft1",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve1",unit_len,unit_divnum,true);

        skin = SKINPARTS_addShaft(skin,"shaft2",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve2",unit_len,unit_divnum,true);

        skin = SKINPARTS_addShaft(skin,"shaft3",unit_len);

        skin = SKINPARTS_addCurve90(skin,"curve3",unit_len,unit_divnum,false);

        skin = SKINPARTS_addShaft(skin,"shaft4",unit_len);

        skin = SKINPARTS_addArrowRoot(skin,arrow_width,unit_len);

        skin = SKINPARTS_addArrowTop(skin);

        skin = SKINPARTS_addArrowHead(skin);

        skin.InsertMidBone("shaft3_shaft");

        return skin;
    }

#endregion

#region スキンからメッシュ生成
    private void CreateMesh(Skin skin, GameObject go)
    {
        // ref https://docs.unity3d.com/jp/540/ScriptReference/Mesh-bindposes.html

        var rend = go.AddComponent<SkinnedMeshRenderer>();

        // Build Mesh
        var vlist = new List<Vector3>();  //頂点
        var tlist = new List<int>();      //三角形インデックス
        var uvlist = new List<Vector2>();
        var bwlist = new List<BoneWeight>();

        skin.pol.MESH_create(skin, ref vlist, ref tlist,ref uvlist, ref bwlist );

        Mesh mesh = new Mesh();
        mesh.vertices = vlist.ToArray();
        mesh.uv = uvlist.ToArray();
        mesh.triangles = tlist.ToArray();
        mesh.RecalculateNormals();

        var material = Resources.Load<Material>("Arrow/Arrow");
        if (material==null)
        {
            material = new Material(Shader.Find("Diffuse"));
        }
        rend.material = material;

        // assign bone weight
        mesh.boneWeights = bwlist.ToArray();

        Transform[] bones;
        Matrix4x4[] bindPoses;

        skin.SKINNEDMESH_createBones( go.transform, out bones, out bindPoses);
        mesh.bindposes = bindPoses;
        rend.bones = bones;
        rend.sharedMesh = mesh;
    }
#endregion

#region 公開
    
    public static GameObject CreateArrowSub(Arrow.TYPE type,float shuft_width, float unit_len, int curve_divnum, float arrow_width)
    {
        var p  = new ArrowMaker();
        var go = new GameObject(type.ToString());
        Skin skin = null;

        switch(type)
        {
            case Arrow.TYPE.ONEWAY:    skin =  p.CreateSkin_OneWay_Arrow  (shuft_width,unit_len,curve_divnum,arrow_width);     break;
            case Arrow.TYPE.TURN_R:    skin =  p.CreateSkin_Curve_R_Arrow (shuft_width,unit_len,curve_divnum,arrow_width);     break;
            case Arrow.TYPE.TURN_L:    skin =  p.CreateSkin_Curve_L_Arrow (shuft_width,unit_len,curve_divnum,arrow_width);     break;
            case Arrow.TYPE.U_TURN_R:  skin =  p.CreateSkin_U_Turn_R_Arrow(shuft_width,unit_len,curve_divnum,arrow_width);     break;
            case Arrow.TYPE.U_TURN_L:  skin =  p.CreateSkin_U_Turn_L_Arrow(shuft_width,unit_len,curve_divnum,arrow_width);     break;
            case Arrow.TYPE.S_TURN_R:  skin =  p.CreateSkin_S_Turn_R_Arrow(shuft_width,unit_len,curve_divnum,arrow_width);     break;
            case Arrow.TYPE.S_TURN_L:  skin =  p.CreateSkin_S_Turn_L_Arrow(shuft_width,unit_len,curve_divnum,arrow_width);     break;
            case Arrow.TYPE.SS_TURN_R: skin =  p.CreateSkin_SS_Turn_R_Arrow(shuft_width,unit_len,curve_divnum,arrow_width);    break;
            case Arrow.TYPE.SS_TURN_L: skin =  p.CreateSkin_SS_Turn_L_Arrow(shuft_width,unit_len,curve_divnum,arrow_width);    break;
            case Arrow.TYPE.HEAD_C_R:  skin =  p.CreateSkin_HEAD_C_R_Arrow(shuft_width,unit_len,curve_divnum,arrow_width);     break;
            case Arrow.TYPE.HEAD_C_L:  skin =  p.CreateSkin_HEAD_C_L_Arrow(shuft_width,unit_len,curve_divnum,arrow_width);     break;
            case Arrow.TYPE.TAIL_C_R:  skin =  p.CreateSkin_TAIL_C_R_Arrow(shuft_width,unit_len,curve_divnum,arrow_width);     break;
            case Arrow.TYPE.TAIL_C_L:  skin =  p.CreateSkin_TAIL_C_L_Arrow(shuft_width,unit_len,curve_divnum,arrow_width);     break;
        }
        p.CreateMesh(skin,go);
        return go;
    }

    public static GameObject CreateArrow(Arrow.TYPE type)
    {
        var go = new GameObject("ARROW");
        var ac = go.AddComponent<Arrow>();
        ac.m_type = type;
        return go;
    }
#endregion
}
