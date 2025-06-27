using System.ComponentModel;
using UnityEngine;
using UnityWeld.Binding;

namespace GameLogic
{
    public abstract class UIViewModelBase : MonoBehaviour, INotifyPropertyChanged, IViewModelProvider
    {
        #region Unity Weld
        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected void RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChanged?.Invoke(this, args);
        }
        
        object IViewModelProvider.GetViewModel()
        {
            return this;
        }

        private string m_viewModelTypeName;
        string IViewModelProvider.GetViewModelTypeName()
        {
            if (string.IsNullOrEmpty(m_viewModelTypeName))
            {
                m_viewModelTypeName = GetType().FullName;
            }
            return m_viewModelTypeName;
        }
        #endregion

        #region LifeCycle

        public virtual void OnRefresh(object userData) { }

        #endregion
    }
}