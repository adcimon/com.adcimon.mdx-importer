#ifndef MDX_STANDARD_CORE_FORWARD_INCLUDED
#define MDX_STANDARD_CORE_FORWARD_INCLUDED

#include "UnityStandardConfig.cginc"

#include "Packages/com.adcimon.mdx-importer/ShaderLibrary/MdxStandardCore.cginc"
VertexOutputForwardBase vertBase (VertexInput v) { return vertForwardBase(v); }
VertexOutputForwardAdd vertAdd (VertexInput v) { return vertForwardAdd(v); }
half4 fragBase (VertexOutputForwardBase i) : SV_Target { return fragForwardBaseInternal(i); }
half4 fragAdd (VertexOutputForwardAdd i) : SV_Target { return fragForwardAddInternal(i); }

#endif // MDX_STANDARD_CORE_FORWARD_INCLUDED
