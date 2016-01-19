using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;

namespace MarkupExtensionsForEvents
{
    [MarkupExtensionReturnType(typeof(EventHandler))]
    public sealed class InvokeCommandExtension : MarkupExtension
    {
        /// <summary>
        /// イベント発生時に呼び出すコマンドのパスを取得または設定します。
        /// </summary>
        public PropertyPath Path { get; set; }

        private TargetObject _targetObject;

        public InvokeCommandExtension(PropertyPath bindingCommandPath)
        {
            this.Path = bindingCommandPath;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var pvt = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;

            if (pvt != null)
            {
                var ei = pvt.TargetProperty as EventInfo;
                var mi = pvt.TargetProperty as MethodInfo;
                var type = (ei != null) ? ei.EventHandlerType :
                                          (mi != null) ? mi.GetParameters()[1].ParameterType :
                                                         null;

                if (type != null)
                {
                    var target = pvt.TargetObject as FrameworkElement;

                    _targetObject = new TargetObject();
                    this.SetBinding(target.DataContext);
                    target.DataContextChanged += (s, e) => { this.SetBinding(e.NewValue); };


                    // ここで、イベントハンドラを作成し、マークアップ拡張の結果として返す
                    var nonGenericMethod = GetType().GetMethod("PrivateHandlerGeneric", BindingFlags.NonPublic | BindingFlags.Instance);
                    var argType = type.GetMethod("Invoke").GetParameters()[1].ParameterType;
                    var genericMethod = nonGenericMethod.MakeGenericMethod(argType);

                    return Delegate.CreateDelegate(type, this, genericMethod);
                }
            }

            return null;
        }

        private void SetBinding(object dataContext)
        {
            var binding = new Binding()
            {
                Source = dataContext,
                Path = this.Path,
            };
            BindingOperations.SetBinding(_targetObject, TargetObject.TargetValueProperty, binding);
        }

        private void PrivateHandlerGeneric<T>(object sender, T e)
        {
            // コマンドの取得
            var command = _targetObject.TargetValue as ICommand;

            // コマンドを呼び出す
            if (command != null && command.CanExecute(e))
            {
                command.Execute(e);
            }
        }
    }

    /// <summary>
    /// 実行対象のコマンドをバインディングターゲットとして保持するために使用するクラス
    /// InvokeCommandExtensionクラス内で使用します
    /// </summary>
    internal class TargetObject : DependencyObject
    {
        public object TargetValue
        {
            get { return (object)GetValue(TargetValueProperty); }
            set { SetValue(TargetValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TargetValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TargetValueProperty =
            DependencyProperty.Register("TargetValue", typeof(object), typeof(TargetObject), new PropertyMetadata(null));
    }
}
