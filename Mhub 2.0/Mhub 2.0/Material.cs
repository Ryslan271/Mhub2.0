//------------------------------------------------------------------------------
// <auto-generated>
//    Этот код был создан из шаблона.
//
//    Изменения, вносимые в этот файл вручную, могут привести к непредвиденной работе приложения.
//    Изменения, вносимые в этот файл вручную, будут перезаписаны при повторном создании кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Mhub_2._0
{
    using System;
    using System.Collections.Generic;
    
    public partial class Material
    {
        public Material()
        {
            this.ProductMaterial = new HashSet<ProductMaterial>();
        }
    
        public int id { get; set; }
        public string Name { get; set; }
        public Nullable<int> IdTypeMaterial { get; set; }
        public Nullable<int> CountPackaged { get; set; }
        public Nullable<int> idUnit { get; set; }
        public Nullable<int> CountInDtock { get; set; }
        public Nullable<int> Min { get; set; }
        public Nullable<int> Price { get; set; }
    
        public virtual TypeMaterial TypeMaterial { get; set; }
        public virtual Unit Unit { get; set; }
        public virtual ICollection<ProductMaterial> ProductMaterial { get; set; }
    }
}
