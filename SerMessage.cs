using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PP_lab2_connect4_1
{
    [Serializable()]
    public class SerMessage : ISerializable
    {
        public string MsgType;
        public string Board;
        public double Evaluation;
        public int MovePlayer;
        public int MoveCPU;
        public int ProcessID;

        public SerMessage()
        {}

        public SerMessage(SerializationInfo info, StreamingContext context)
        {
            MsgType = (String)info.GetValue("MsgType", typeof(string));
            Board = (String)info.GetValue("Board", typeof(string));
            Evaluation = (double)info.GetValue("Evaluation", typeof(double));
            MovePlayer = (int)info.GetValue("MovePlayer", typeof(int));
            MoveCPU = (int)info.GetValue("MoveCPU", typeof(int));
            ProcessID = (int)info.GetValue("ProcessID", typeof(int));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("MsgType", MsgType);
            info.AddValue("Board", Board);
            info.AddValue("Evaluation", Evaluation);
            info.AddValue("MovePlayer", MovePlayer);
            info.AddValue("MoveCPU", MoveCPU);
            info.AddValue("ProcessID", ProcessID);
        }
    }
}
