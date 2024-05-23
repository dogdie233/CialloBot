using System.Reflection;
using CialloBot.Plugin;

namespace CialloBot.Test
{
    public class PluginServiceContainerTest
    {
        public interface IInterface1 { }
        public class Class1A : IInterface1 { }
        public class Class1B : IInterface1 { }
        public class PureClass { }

        [Fact]
        public void RegisterAndGetSameType_ReturnSame()
        {
            var provider = new SharedServiceContainer();
            var regType = typeof(IInterface1);
            var getType = regType;
            var impl = new Class1A();

            provider.RegisterService("test", regType, impl);
            var serviceGot = provider.GetService(getType);

            Assert.Same(impl, serviceGot);
        }

        [Fact]
        public void RegisterAndGetDerivedType_ReturnNull()
        {
            var provider = new SharedServiceContainer();
            var regType = typeof(IInterface1);
            var getType = typeof(Class1A);
            var impl = new Class1A();

            provider.RegisterService("test", regType, impl);
            var serviceGot = provider.GetService(getType);

            Assert.Null(serviceGot);
        }

        [Fact]
        public void RegisterAndGetDifferentType_ReturnNull()
        {
            var provider = new SharedServiceContainer();
            var regType = typeof(IInterface1);
            var getType = typeof(PureClass);
            var impl = new PureClass();

            provider.RegisterService("test", regType, impl);
            var serviceGot = provider.GetService(getType);

            Assert.Null(serviceGot);
        }

        [Fact]
        public void RegisterAndGetRequiredSameType_ReturnSame()
        {
            var provider = new SharedServiceContainer();
            var serviceType = typeof(IInterface1);
            var impl = new PureClass();

            provider.RegisterService("test", serviceType, impl);
            var serviceGot = provider.GetRequiredService(serviceType);

            Assert.Same(impl, serviceGot);
        }

        [Fact]
        public void RegisterTwoServiceWithSameServiceType_ReturnTailService()
        {
            var provider = new SharedServiceContainer();
            var regType = typeof(IInterface1);
            var getType = typeof(IInterface1);
            var implA = new Class1A();
            var implB = new Class1A();
            var privateContainer = GetContainer(provider);

            provider.RegisterService("test", regType, implA);
            provider.RegisterService("test", regType, implB);
            var serviceGot = provider.GetService(getType);

            Assert.Same(implB, serviceGot);
        }

        [Fact]
        public void GetRequiredNotRegisteredType_ThrowIOE()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var provider = new SharedServiceContainer();
                var serviceType = typeof(IInterface1);
                var regType = typeof(PureClass);
                var impl = new PureClass();

                provider.RegisterService("test", regType, impl);
                var serviceGot = provider.GetRequiredService(serviceType);
            });
        }

        [Fact]
        public void GetRequiredNotRegisteredDerivedType_ThrowIOE()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var provider = new SharedServiceContainer();
                var regType = typeof(IInterface1);
                var getType = typeof(PureClass);
                var impl = new PureClass();

                provider.RegisterService("test", regType, impl);
                var serviceGot = provider.GetRequiredService(getType);
            });
        }

        [Fact]
        public void UnregisterNotExistService_Ok()
        {
            var provider = new SharedServiceContainer();
            var impl = new PureClass();

            provider.Unregister("notfound");
        }

        [Fact]
        public void SameRegistrarRegisterTwoServiceAndRemove()
        {
            var provider = new SharedServiceContainer();
            var regType = typeof(IInterface1);
            var getType = regType;
            var implA = new Class1A();
            var implB = new Class1A();
            var privateContainer = GetContainer(provider);
            var registrarId = "test";

            provider.RegisterService(registrarId, regType, implA);
            provider.RegisterService(registrarId, regType, implB);
            provider.Unregister(registrarId);

            Assert.Empty(privateContainer);
        }

        [Fact]
        public void DifferentRegistrarRegisterSameServiceAndRemove()
        {
            var provider = new SharedServiceContainer();
            var regType = typeof(IInterface1);
            var getType = regType;
            var implA = new Class1A();
            var implB = new Class1A();
            var privateContainer = GetContainer(provider);

            provider.RegisterService("test", regType, implA);
            provider.RegisterService("test2", regType, implB);
            provider.Unregister("test");
            var serviceGot = provider.GetService(getType);

            Assert.Same(implB, serviceGot);
        }

        private Dictionary<Type, List<SharedServiceContainer.ServiceDescription>> GetContainer(SharedServiceContainer provider)
            => (Dictionary<Type, List<SharedServiceContainer.ServiceDescription>>)provider.GetType().GetField("servicesContainer", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(provider)!;
    }
}