using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace I2VISTools.Tools
{

    public class PropertyChanges
    {
        public Dictionary<string, List<string>> OldValues = new Dictionary<string, List<string>>();
        public Dictionary<string, List<string>> NewValues = new Dictionary<string, List<string>>();

        public void Add(string property, List<string> oldVals, List<string> newVals)
        {
            OldValues.Add(property, oldVals);
            NewValues.Add(property, newVals);
        }
    }

    public class HistoryLog
    {

        public HistoryLog()
        {
            //_creationTime = DateTime.Now;
            //Changes = new List<PropertyChanges>();
        }

        public HistoryLog(Action doAction, Action undoAction)
        {
            Do = doAction;
            Undo = undoAction;
        }

        public Action Do;
        public Action Undo;

        //public HistoryLog(object oldVal, object newVal) : this()
        //{
        //    OldValue = oldVal;
        //    NewValue = newVal;
        //}

        //private DateTime _creationTime;

        //public object Object { get; set; }
        //public string ObjectName { get; set; }
        //public string Operation { get; set; }
        //public List<PropertyChanges> Changes { get; set; }

        //public object NewValue;
        //public object OldValue;

        //public DateTime Time
        //{
        //    get { return _creationTime; }
        //}

    }

    public class HIstoryView
    {
        private List<HistoryLog> _logs = new List<HistoryLog>(100);
        public List<HistoryLog> Logs { get { return _logs; } }

        private int _logsCount = 0;
        private int _currentPosition;

        public void Add(HistoryLog log)
        {
            if (_currentPosition == -1) _logs.Clear();

            if (_currentPosition != _logs.Count - 1)
            {
                _logs.RemoveRange(_currentPosition, _logs.Count - _currentPosition);
            }

            if (_logsCount >= 100) _logs.RemoveAt(0);
            _logsCount++;
            _logs.Add(log);
            _currentPosition = _logs.Count-1;
        }

        

        //public void Add(object obj, string objName, string operation, List<string> oldValues, List<string> newValues)
        //{
        //    var log = new HistoryLog
        //    {
        //        Object = obj,
        //        ObjectName = objName,
        //        Operation = operation,
        //        OldValues = oldValues,
        //        NewValues = newValues
        //    };
        //    Add(log);
        //}

        public void Undo()
        {
            if (_currentPosition < 0) return;

            _logs[_currentPosition].Undo();
            _currentPosition--;
        }

        public void Redo()
        {
            if (_currentPosition == _logs.Count-1) return;
            _currentPosition++;
            _logs[_currentPosition].Do();
        }

        public HistoryLog ShowNextUndo()
        {
            if (_currentPosition < 0 || _currentPosition > _logs.Count) return null;
            if (_currentPosition != 0)
            {
                _currentPosition--;
            }
            else
            {
                return _logs[0];
            }
            return _logs[_currentPosition + 1];
        }

        public HistoryLog ShowNextRedo()
        {
            if (_currentPosition < 0 || _currentPosition > _logs.Count-1) return null;
            if (_currentPosition != _logs.Count - 1)
            {
                _currentPosition++;
            }
            else
            {
                return _logs.Last();
            }
            
            return _logs[_currentPosition - 1];
        }

    }
}
