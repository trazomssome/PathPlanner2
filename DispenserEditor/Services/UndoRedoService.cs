using System.Collections.Generic;
using DispenserEditor.Models;
using Newtonsoft.Json;

namespace DispenserEditor.Services
{
    public class UndoRedoService
    {
        private readonly List<string> _history = new List<string>();
        private int _index = -1;

        public bool CanUndo => _index > 0;
        public bool CanRedo => _index >= 0 && _index < _history.Count - 1;

        public void Reset(IntegratedRecipe state)
        {
            _history.Clear();
            _history.Add(JsonConvert.SerializeObject(state));
            _index = 0;
        }

        public void Record(IntegratedRecipe state)
        {
            var snapshot = JsonConvert.SerializeObject(state);
            if (_index < _history.Count - 1)
            {
                _history.RemoveRange(_index + 1, _history.Count - _index - 1);
            }

            _history.Add(snapshot);
            _index = _history.Count - 1;
        }

        public IntegratedRecipe Undo()
        {
            if (!CanUndo)
            {
                return null;
            }

            _index--;
            return JsonConvert.DeserializeObject<IntegratedRecipe>(_history[_index]);
        }

        public IntegratedRecipe Redo()
        {
            if (!CanRedo)
            {
                return null;
            }

            _index++;
            return JsonConvert.DeserializeObject<IntegratedRecipe>(_history[_index]);
        }
    }
}


