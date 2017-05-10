#if OPENGL
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lime
{
	[Flags]
	public enum ShaderOptions
	{
		None = 0,
		UseAlphaTexture1 = 1,
		UseAlphaTexture2 = 2,
		PremultiplyAlpha = 4,
		VertexAnimation = 8,
		Count = 4,
	}

	public class ShaderPrograms
	{
		public static class Attributes
		{
			public const int Pos1 = 0;
			public const int Color1 = 1;

			public const int UV1 = 2;
			public const int UV2 = 3;
			public const int UV3 = 4;
			public const int UV4 = 5;
			public const int BlendIndices = 6;
			public const int BlendWeights = 7;

			public const int Pos2 = 8;
			public const int Color2 = 9;

			public static IEnumerable<ShaderProgram.AttribLocation> GetLocations()
			{
				return new ShaderProgram.AttribLocation[] {
					new ShaderProgram.AttribLocation { Index = Pos1, Name = "inPos" },
					new ShaderProgram.AttribLocation { Index = Color1, Name = "inColor" },
					new ShaderProgram.AttribLocation { Index = UV1, Name = "inTexCoords1" },
					new ShaderProgram.AttribLocation { Index = UV2, Name = "inTexCoords2" },
					new ShaderProgram.AttribLocation { Index = UV3, Name = "inTexCoords3" },
					new ShaderProgram.AttribLocation { Index = UV4, Name = "inTexCoords4" },
					new ShaderProgram.AttribLocation { Index = BlendIndices, Name = "inBlendIndices" },
					new ShaderProgram.AttribLocation { Index = BlendWeights, Name = "inBlendWeights" },
					new ShaderProgram.AttribLocation { Index = Pos2, Name = "inPos2" },
					new ShaderProgram.AttribLocation { Index = Color2, Name = "inColor2" },
				};
			}
		}

		class CustomShaderProgram
		{
			private ShaderProgram[] programs;
			private string vertexShader;
			private string fragmentShader;
			private IEnumerable<ShaderProgram.AttribLocation> attribLocations;
			private IEnumerable<ShaderProgram.Sampler> samplers;

			public CustomShaderProgram(string vertexShader, string fragmentShader, IEnumerable<ShaderProgram.AttribLocation> attribLocations, IEnumerable<ShaderProgram.Sampler> samplers)
			{
				this.vertexShader = vertexShader;
				this.fragmentShader = fragmentShader;
				this.attribLocations = attribLocations;
				this.samplers = samplers;
				this.programs = new ShaderProgram[1 << (int)ShaderOptions.Count];
			}

			public ShaderProgram GetProgram(ShaderOptions options)
			{
				var program = programs[(int)options];
				if (program == null) {
					var preamble = CreateShaderPreamble(options);
					programs[(int)options] = program = new ShaderProgram(
						new Shader[] {
							new VertexShader(preamble + vertexShader),
							new FragmentShader(preamble + fragmentShader)
						},
						attribLocations, samplers);
				}
				return program;
			}

			private string CreateShaderPreamble(ShaderOptions options)
			{
				string result = "";
				int bit = 1;
				while (options != ShaderOptions.None) {
					if (((int)options & 1) == 1) {
						var name = Enum.GetName(typeof(ShaderOptions), (ShaderOptions)bit);
						result += "#define " + name + "\n";
					}
					bit <<= 1;
					options = (ShaderOptions)((int)options >> 1);
				}
				return result;
			}
		}

		public static ShaderPrograms Instance = new ShaderPrograms();

		private ShaderPrograms()
		{
			colorOnlyBlendingProgram = CreateShaderProgram(oneTextureVertexShader, colorOnlyFragmentShader);
			oneTextureBlengingProgram = CreateShaderProgram(oneTextureVertexShader, oneTextureFragmentShader);
			twoTexturesBlengingProgram = CreateShaderProgram(twoTexturesVertexShader, twoTexturesFragmentShader);
			silhuetteBlendingProgram = CreateShaderProgram(oneTextureVertexShader, silhouetteFragmentShader);
			twoTexturesSilhuetteBlendingProgram = CreateShaderProgram(twoTexturesVertexShader, twoTexturesSilhouetteFragmentShader);
			inversedSilhuetteBlendingProgram = CreateShaderProgram(oneTextureVertexShader, inversedSilhouetteFragmentShader);
		}

		public ShaderProgram GetShaderProgram(ShaderId shader, int numTextures, ShaderOptions options)
		{
			return GetShaderProgram(shader, numTextures).GetProgram(options);
		}

		private CustomShaderProgram GetShaderProgram(ShaderId shader, int numTextures)
		{
			if (shader == ShaderId.Diffuse || shader == ShaderId.Inherited) {
				if (numTextures == 1) {
					return oneTextureBlengingProgram;
				} else if (numTextures == 2) {
					return twoTexturesBlengingProgram;
				}
			} else if (shader == ShaderId.Silhuette) {
				if (numTextures == 1) {
					return silhuetteBlendingProgram;
				} else if (numTextures == 2) {
					return twoTexturesSilhuetteBlendingProgram;
				}
			} else if (shader == ShaderId.InversedSilhuette && numTextures == 1) {
				return inversedSilhuetteBlendingProgram;
			}
			return colorOnlyBlendingProgram;
		}

		readonly string oneTextureVertexShader = @"
			attribute vec4 inPos;
			attribute vec4 inPos2;
			attribute vec4 inColor;
			attribute vec4 inColor2;
			attribute vec2 inTexCoords1;
			varying lowp vec4 color;
			varying lowp vec2 texCoords;
			uniform mat4 matProjection;
			uniform mat4 globalTransform;
			uniform vec4 globalColor;
			uniform highp float morphKoeff;
			void main()
			{
				$ifdef VertexAnimation
					gl_Position = matProjection * (globalTransform * vec4((1.0 - morphKoeff) * inPos + morphKoeff * inPos2));
					color = ((1.0 - morphKoeff) * inColor + morphKoeff * inColor2) * globalColor;
				$else
					gl_Position = matProjection * inPos;
					color = inColor;
				$endif
				texCoords = inTexCoords1;
			}";

		readonly string twoTexturesVertexShader = @"
			attribute vec4 inPos;
			attribute vec4 inPos2;
			attribute vec4 inColor;
			attribute vec4 inColor2;
			attribute vec2 inTexCoords1;
			attribute vec2 inTexCoords2;
			varying lowp vec4 color;
			varying lowp vec2 texCoords1;
			varying lowp vec2 texCoords2;
			uniform mat4 matProjection;
			uniform mat4 globalTransform;
			uniform vec4 globalColor;
			uniform highp float morphKoeff;
			void main()
			{
				$ifdef VertexAnimation
					gl_Position = matProjection * (globalTransform * vec4((1.0 - morphKoeff) * inPos + morphKoeff * inPos2));
					color = ((1.0 - morphKoeff) * inColor + morphKoeff * inColor2) * globalColor;
				$else
					gl_Position = matProjection * inPos;
					color = inColor;
				$endif
				texCoords1 = inTexCoords1;
				texCoords2 = inTexCoords2;
			}";

		readonly string colorOnlyFragmentShader = @"
			varying lowp vec4 color;
			void main()
			{
				gl_FragColor = color;
			}";

		readonly string oneTextureFragmentShader = @"
			varying lowp vec4 color;
			varying lowp vec2 texCoords;
			uniform lowp sampler2D tex1;
			uniform lowp sampler2D tex1a;
			void main()
			{
				lowp vec4 t1 = texture2D(tex1, texCoords);
				$ifdef UseAlphaTexture1
					t1.a = texture2D(tex1a, texCoords).r;
				$endif
				gl_FragColor = color * t1;
				$ifdef PremultiplyAlpha
					gl_FragColor.rgb *= gl_FragColor.a;
				$endif
			}";

		readonly string twoTexturesFragmentShader = @"
			varying lowp vec4 color;
			varying lowp vec2 texCoords1;
			varying lowp vec2 texCoords2;
			uniform lowp sampler2D tex1;
			uniform lowp sampler2D tex2;
			uniform lowp sampler2D tex1a;
			uniform lowp sampler2D tex2a;
			void main()
			{
				lowp vec4 t1 = texture2D(tex1, texCoords1);
				lowp vec4 t2 = texture2D(tex2, texCoords2);
				$ifdef UseAlphaTexture1
					t1.a = texture2D(tex1a, texCoords1).r;
				$endif
				$ifdef UseAlphaTexture2
					t2.a = texture2D(tex2a, texCoords2).r;
				$endif
				gl_FragColor = color * t1 * t2;
				$ifdef PremultiplyAlpha
					gl_FragColor.rgb *= gl_FragColor.a;
				$endif
			}";

		readonly string silhouetteFragmentShader = @"
			varying lowp vec4 color;
			varying lowp vec2 texCoords;
			uniform lowp sampler2D tex1;
			uniform lowp sampler2D tex1a;
			void main()
			{
				$ifdef UseAlphaTexture1
					lowp float a = texture2D(tex1a, texCoords).r;
				$else
					lowp float a = texture2D(tex1, texCoords).a;
				$endif
				gl_FragColor = color * vec4(1.0, 1.0, 1.0, a);
			}";

		readonly string twoTexturesSilhouetteFragmentShader = @"
			varying lowp vec4 color;
			varying lowp vec2 texCoords1;
			varying lowp vec2 texCoords2;
			uniform lowp sampler2D tex1;
			uniform lowp sampler2D tex1a;
			uniform lowp sampler2D tex2;
			uniform lowp sampler2D tex2a;
			void main()
			{
				lowp vec4 t1 = texture2D(tex1, texCoords1);
				$ifdef UseAlphaTexture1
					t1.a = texture2D(tex1a, texCoords1).r;
				$endif
				$ifdef UseAlphaTexture2
					lowp float a2 = texture2D(tex2a, texCoords2).r;
				$else
					lowp float a2 = texture2D(tex2, texCoords2).a;
				$endif
				gl_FragColor = t1 * color * vec4(1.0, 1.0, 1.0, a2);
			}";

		readonly string inversedSilhouetteFragmentShader = @"
			varying lowp vec4 color;
			varying lowp vec2 texCoords;
			uniform lowp sampler2D tex1;
			uniform lowp sampler2D tex1a;
			void main()
			{
				$ifdef UseAlphaTexture1
					lowp float a = 1.0 - texture2D(tex1a, texCoords).r;
				$else
					lowp float a = 1.0 - texture2D(tex1, texCoords).a;
				$endif
				gl_FragColor = color * vec4(1.0, 1.0, 1.0, a);
			}";

		private readonly CustomShaderProgram colorOnlyBlendingProgram;
		private readonly CustomShaderProgram oneTextureBlengingProgram;
		private readonly CustomShaderProgram twoTexturesBlengingProgram;
		private readonly CustomShaderProgram silhuetteBlendingProgram;
		private readonly CustomShaderProgram twoTexturesSilhuetteBlendingProgram;
		private readonly CustomShaderProgram inversedSilhuetteBlendingProgram;

		private static CustomShaderProgram CreateShaderProgram(string vertexShader, string fragmentShader)
		{
			// #ifdef - breaks Unity3D compiler
			vertexShader = vertexShader.Replace('$', '#');
			fragmentShader = fragmentShader.Replace('$', '#');
			return new CustomShaderProgram(vertexShader, fragmentShader, Attributes.GetLocations(), GetSamplers());
		}

		public static IEnumerable<ShaderProgram.Sampler> GetSamplers()
		{
			return new ShaderProgram.Sampler[] {
				new ShaderProgram.Sampler { Name = "tex1", Stage = 0 },
				new ShaderProgram.Sampler { Name = "tex2", Stage = 1 },
				new ShaderProgram.Sampler { Name = "tex1a", Stage = 2 },
				new ShaderProgram.Sampler { Name = "tex2a", Stage = 3 }
			};
		}

		/// <summary>
		/// Colorizes <see cref="SimpleText"/>'s or <see cref="RichText"/>'s grayscale font.
		/// </summary>
		/// <remarks>
		/// To apply this shader you need:
		/// 1. Non-ETC1 font texture.
		/// 2. Data/gradient_map.png file with size of 256x256 and ARGB8 compression.
		/// 3. Use <see cref="Node.Tag"/> field of <see cref="SimpleText"/> or <see cref="TextStyle"/>
		///    to specify color row from this file.
		/// </remarks>
		public class ColorfulTextShaderProgram : ShaderProgram
		{
			private static string vertexShaderText = @"
			attribute vec4 inPos;
			attribute vec4 inPos2;
			attribute vec4 inColor;
			attribute vec4 inColor2;
			attribute vec2 inTexCoords1;
			attribute vec2 inTexCoords2;
			varying lowp vec4 color;
			varying lowp vec2 texCoords1;
			varying lowp vec2 texCoords2;
			uniform mat4 matProjection;
			uniform mat4 globalTransform;
			uniform vec4 globalColor;
			void main()
			{
				gl_Position = matProjection * inPos;
				color = inColor;
				texCoords1 = inTexCoords1;
				texCoords2 = inTexCoords2;
			}";

			private static string fragmentShaderText = $@"
			varying lowp vec4 color;
			varying lowp vec2 texCoords1;
			varying lowp vec2 texCoords2;
			uniform lowp sampler2D tex1;
			uniform lowp sampler2D tex2;
			uniform lowp sampler2D tex1a;
			uniform lowp sampler2D tex2a;
			uniform lowp float colorIndex;
			void main()
			{{
				lowp vec4 t1 = texture2D(tex1, texCoords1);
				lowp vec4 t2 = texture2D(tex2, vec2(t1.x, colorIndex));
				gl_FragColor = color * vec4(t2.rgb, t1.a * t2.a);
			}}";

			public ColorfulTextShaderProgram()
				: base(GetShaders(), ShaderPrograms.Attributes.GetLocations(), ShaderPrograms.GetSamplers())
			{ }

			protected override void LoadUniformValues()
			{
				base.LoadUniformValues();
				LoadBoolean(androidHackUniformId, AndroidHack);
				LoadFloat(colorIndexUniformId, ColorIndex);
			}

			private int androidHackUniformId;
			private int colorIndexUniformId;

			public bool AndroidHack { get; set; }
			public float ColorIndex { get; set; }

			protected override void InitializeUniformIds()
			{
				base.InitializeUniformIds();
				Lime.Application.InvokeOnMainThread(() => {
					androidHackUniformId = GetUniformId("androidHack");
					colorIndexUniformId = GetUniformId("colorIndex");
				});
			}

			private static IEnumerable<Shader> GetShaders()
			{
				return new Shader[] {
				new VertexShader(vertexShaderText.Replace('$', '#')),
				new FragmentShader(fragmentShaderText.Replace('$', '#'))
			};
			}

			private static ITexture fontGradientTexture;
			private static readonly List<ColorfulTextShaderProgram> cachedShaderPrograms = new List<ColorfulTextShaderProgram>();
			private static ColorfulTextShaderProgram GetShaderProgram(int styleIndex)
			{
				if (cachedShaderPrograms.Count <= styleIndex) {
					for (int i = 0; i <= styleIndex; i++) {
						if (i < cachedShaderPrograms.Count) {
							continue;
						}
						cachedShaderPrograms.Add(new ColorfulTextShaderProgram { ColorIndex = (i * 2 + 1) / 512.0f });
					}
				}
				return cachedShaderPrograms[styleIndex];
			}
			private static ITexture GradientRampTexture
			{
				get {
					return fontGradientTexture = fontGradientTexture ?? new SerializableTexture("Fonts/gradient_map");
				}
			}

			public static void HandleSimpleTextSprite(SimpleText text, Sprite sprite)
			{
				if (text.ShaderProgram != null) {
					sprite.ShaderProgram = text.ShaderProgram;
					sprite.Texture2 = GradientRampTexture;
					return;
				}
				if (!string.IsNullOrEmpty(text.Tag)) {
					text.ShaderProgram = sprite.ShaderProgram = GetShaderProgram(int.Parse(text.Tag));
					sprite.Texture2 = GradientRampTexture;
				}
			}

			public static void HandleRichTextSprite(RichText text, Sprite sprite)
			{
				var style = text.Renderer.Styles[sprite.Tag];
				if (style.ShaderProgram != null) {
					sprite.ShaderProgram = style.ShaderProgram;
					sprite.Texture2 = GradientRampTexture;
					return;
				}
				if (!string.IsNullOrEmpty(style.Tag)) {
					style.ShaderProgram = sprite.ShaderProgram = GetShaderProgram(int.Parse(style.Tag));
					sprite.Texture2 = GradientRampTexture;
				}
			}
		}
	}
}
#endif
