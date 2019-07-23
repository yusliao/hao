using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util.Configuration;

namespace PushServer.Configuration
{
    [System.Configuration.ConfigurationCollection(typeof(ClientElement), AddItemName = "client")]
    public class ClientCollection : GenericConfigurationElementCollection<ClientElement, IClientConfig>
    {
        public void AddNew(ClientElement client)
        {
            base.BaseAdd(client);
        }
        public void Remove(string name)
        {
            base.BaseRemove(name);
        }
    }
    //[System.Configuration.ConfigurationCollection(typeof(ClientElement), AddItemName = "client")]
    //public class ClientCollection : ConfigurationElementCollection
    //{
    //    /// <summary>
    //    /// Gets the element key.
    //    /// </summary>
    //    /// <param name="element">The element.</param>
    //    /// <returns></returns>
    //    protected override object GetElementKey(ConfigurationElement element)
    //    {
    //        return ((ClientElement)element).Name;
    //    }
    //    public ClientCollection()
    //    {
    //        // Add one url to the collection.  This is
    //        // not necessary; could leave the collection 
    //        // empty until items are added to it outside
    //        // the constructor.
    //        ClientElement url =
    //            (ClientElement)CreateNewElement();
    //        Add(url);
    //    }

    //    public override
    //        ConfigurationElementCollectionType CollectionType
    //    {
    //        get
    //        {
    //            return

    //                ConfigurationElementCollectionType.AddRemoveClearMap;
    //        }
    //    }

    //    protected override
    //        ConfigurationElement CreateNewElement()
    //    {
    //        return new ClientElement();
    //    }


    //    protected override
    //        ConfigurationElement CreateNewElement(
    //        string elementName)
    //    {
    //        return new ClientElement(elementName);
    //    }





    //    public new string AddElementName
    //    {
    //        get
    //        { return base.AddElementName; }

    //        set
    //        { base.AddElementName = value; }

    //    }

    //    public new string ClearElementName
    //    {
    //        get
    //        { return base.ClearElementName; }

    //        set
    //        { base.ClearElementName = value; }

    //    }

    //    public new string RemoveElementName
    //    {
    //        get
    //        { return base.RemoveElementName; }
    //    }

    //    public new int Count
    //    {
    //        get { return base.Count; }
    //    }


    //    public ClientElement this[int index]
    //    {
    //        get
    //        {
    //            return (ClientElement)BaseGet(index);
    //        }
    //        set
    //        {
    //            if (BaseGet(index) != null)
    //            {
    //                BaseRemoveAt(index);
    //            }
    //            BaseAdd(index, value);
    //        }
    //    }

    //    new public ClientElement this[string Name]
    //    {
    //        get
    //        {
    //            return (ClientElement)BaseGet(Name);
    //        }
    //    }

    //    public int IndexOf(ClientElement url)
    //    {
    //        return BaseIndexOf(url);
    //    }

    //    public void Add(ClientElement url)
    //    {
    //        BaseAdd(url);
    //        // Add custom code here.
    //    }

    //    protected override void
    //        BaseAdd(ConfigurationElement element)
    //    {
    //        BaseAdd(element, false);
    //        // Add custom code here.
    //    }

    //    public void Remove(ClientElement url)
    //    {
    //        if (BaseIndexOf(url) >= 0)
    //            BaseRemove(url.Name);
    //    }

    //    public void RemoveAt(int index)
    //    {
    //        BaseRemoveAt(index);
    //    }

    //    public void Remove(string name)
    //    {
    //        BaseRemove(name);
    //    }

    //    public void Clear()
    //    {
    //        BaseClear();
    //        // Add custom code here.
    //    }
    //}

}
