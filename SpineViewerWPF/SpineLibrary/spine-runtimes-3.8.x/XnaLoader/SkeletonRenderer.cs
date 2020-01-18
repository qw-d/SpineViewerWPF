/******************************************************************************
 * Spine Runtimes License Agreement
 * Last updated May 1, 2019. Replaces all prior versions.
 *
 * Copyright (c) 2013-2019, Esoteric Software LLC
 *
 * Integration of the Spine Runtimes into software or otherwise creating
 * derivative works of the Spine Runtimes is permitted under the terms and
 * conditions of Section 2 of the Spine Editor License Agreement:
 * http://esotericsoftware.com/spine-editor-license
 *
 * Otherwise, it is permitted to integrate the Spine Runtimes into software
 * or otherwise create derivative works of the Spine Runtimes (collectively,
 * "Products"), provided that each user of the Products must obtain their own
 * Spine Editor license and redistribution of the Products in any form must
 * include this license and copyright notice.
 *
 * THIS SOFTWARE IS PROVIDED BY ESOTERIC SOFTWARE LLC "AS IS" AND ANY EXPRESS
 * OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN
 * NO EVENT SHALL ESOTERIC SOFTWARE LLC BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES, BUSINESS
 * INTERRUPTION, OR LOSS OF USE, DATA, OR PROFITS) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
 * EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *****************************************************************************/

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Spine3_8_x {
	/// <summary>Draws region and mesh attachments.</summary>
	public class SkeletonRenderer {
		private const int TL = 0;
		private const int TR = 1;
		private const int BL = 2;
		private const int BR = 3;

		SkeletonClipping clipper = new SkeletonClipping();
		GraphicsDevice device;
		MeshBatcher batcher;
		public MeshBatcher Batcher { get { return batcher; } }
		RasterizerState rasterizerState;
		float[] vertices = new float[8];
		int[] quadTriangles = { 0, 1, 2, 2, 3, 0 };
		BlendState defaultBlendState;

		Effect effect;
		public Effect Effect { get { return effect; } set { effect = value; } }
		public IVertexEffect VertexEffect { get; set; }

		private bool premultipliedAlpha;
		public bool PremultipliedAlpha { get { return premultipliedAlpha; } set { premultipliedAlpha = value; } }

		public SkeletonRenderer (GraphicsDevice device) {
			this.device = device;

			batcher = new MeshBatcher();

			var basicEffect = new BasicEffect(device);
			basicEffect.World = Matrix.Identity;
			basicEffect.View = Matrix.CreateLookAt(new Vector3(0.0f, 0.0f, 1.0f), Vector3.Zero, Vector3.Up);
			basicEffect.TextureEnabled = true;
			basicEffect.VertexColorEnabled = true;
			effect = basicEffect;

			rasterizerState = new RasterizerState();
			rasterizerState.CullMode = CullMode.None;

			Bone.yDown = true;
		}

		public void Begin () {
			defaultBlendState = premultipliedAlpha ? BlendState.AlphaBlend : BlendState.NonPremultiplied;

			device.RasterizerState = rasterizerState;
			device.BlendState = defaultBlendState;
		}

		public void End () {
			foreach (EffectPass pass in effect.CurrentTechnique.Passes) {
				pass.Apply();
				batcher.Draw(device);
			}
			batcher.AfterLastDrawPass();
		}

		public void Draw(Skeleton skeleton) {
			var drawOrder = skeleton.DrawOrder;
			var drawOrderItems = skeleton.DrawOrder.Items;
			float skeletonR = skeleton.R, skeletonG = skeleton.G, skeletonB = skeleton.B, skeletonA = skeleton.A;
			Color color = new Color();

			if (VertexEffect != null) VertexEffect.Begin(skeleton);

			for (int i = 0, n = drawOrder.Count; i < n; i++) {
				Slot slot = drawOrderItems[i];
				Attachment attachment = slot.Attachment;

				float attachmentColorR, attachmentColorG, attachmentColorB, attachmentColorA;
				Texture2D texture = null;
				int verticesCount = 0;
				float[] vertices = this.vertices;
				int indicesCount = 0;
				int[] indices = null;
				float[] uvs = null;

				if (attachment is RegionAttachment) {
					RegionAttachment regionAttachment = (RegionAttachment)attachment;
					attachmentColorR = regionAttachment.R; attachmentColorG = regionAttachment.G; attachmentColorB = regionAttachment.B; attachmentColorA = regionAttachment.A;
					AtlasRegion region = (AtlasRegion)regionAttachment.RendererObject;
					texture = (Texture2D)region.page.rendererObject;
					verticesCount = 4;
					regionAttachment.ComputeWorldVertices(slot.Bone, vertices, 0, 2);
					indicesCount = 6;
					indices = quadTriangles;
					uvs = regionAttachment.UVs;
				}
				else if (attachment is MeshAttachment) {
					MeshAttachment mesh = (MeshAttachment)attachment;
					attachmentColorR = mesh.R; attachmentColorG = mesh.G; attachmentColorB = mesh.B; attachmentColorA = mesh.A;
					AtlasRegion region = (AtlasRegion)mesh.RendererObject;
					texture = (Texture2D)region.page.rendererObject;
					int vertexCount = mesh.WorldVerticesLength;
					if (vertices.Length < vertexCount) vertices = new float[vertexCount];
					verticesCount = vertexCount >> 1;
					mesh.ComputeWorldVertices(slot, vertices);
					indicesCount = mesh.Triangles.Length;
					indices = mesh.Triangles;
					uvs = mesh.UVs;
				}
				else if (attachment is ClippingAttachment) {
					ClippingAttachment clip = (ClippingAttachment)attachment;
					clipper.ClipStart(slot, clip);
					continue;
				}
				else {
					continue;
				}

				// set blend state
                BlendState blendState = new BlendState();
                Blend blendSrc;
                Blend blendDst;
                if (premultipliedAlpha)
                {
                    blendState.AlphaBlendFunction = BlendState.AlphaBlend.AlphaBlendFunction;
                    blendState.BlendFactor = BlendState.AlphaBlend.BlendFactor;
                    blendState.ColorBlendFunction = BlendState.AlphaBlend.ColorBlendFunction;
                    blendState.ColorWriteChannels = BlendState.AlphaBlend.ColorWriteChannels;
                    blendState.ColorWriteChannels1 = BlendState.AlphaBlend.ColorWriteChannels1;
                    blendState.ColorWriteChannels2 = BlendState.AlphaBlend.ColorWriteChannels2;
                    blendState.ColorWriteChannels3 = BlendState.AlphaBlend.ColorWriteChannels3;
                    blendState.MultiSampleMask = BlendState.AlphaBlend.MultiSampleMask;
                }
                else
                {
                    blendState.AlphaBlendFunction = BlendState.NonPremultiplied.AlphaBlendFunction;
                    blendState.BlendFactor = BlendState.NonPremultiplied.BlendFactor;
                    blendState.ColorBlendFunction = BlendState.NonPremultiplied.ColorBlendFunction;
                    blendState.ColorWriteChannels = BlendState.NonPremultiplied.ColorWriteChannels;
                    blendState.ColorWriteChannels1 = BlendState.NonPremultiplied.ColorWriteChannels1;
                    blendState.ColorWriteChannels2 = BlendState.NonPremultiplied.ColorWriteChannels2;
                    blendState.ColorWriteChannels3 = BlendState.NonPremultiplied.ColorWriteChannels3;
                    blendState.MultiSampleMask = BlendState.NonPremultiplied.MultiSampleMask;
                }
                switch (slot.Data.BlendMode)
                {
                    case BlendMode.Additive:
                        blendState = BlendState.Additive;
                        break;
                    case BlendMode.Multiply:
                        blendSrc = BlendXna.GetXNABlend(BlendXna.GL_DST_COLOR);
                        blendDst = BlendXna.GetXNABlend(BlendXna.GL_ONE_MINUS_SRC_ALPHA);
                        blendState.ColorSourceBlend = blendSrc;
                        blendState.AlphaSourceBlend = blendSrc;
                        blendState.ColorDestinationBlend = blendDst;
                        blendState.AlphaDestinationBlend = blendDst;
                        break;
                    case BlendMode.Screen:
                        blendSrc = BlendXna.GetXNABlend(premultipliedAlpha ? BlendXna.GL_ONE : BlendXna.GL_SRC_ALPHA);
                        blendDst = BlendXna.GetXNABlend(BlendXna.GL_ONE_MINUS_SRC_COLOR);
                        blendState.ColorSourceBlend = blendSrc;
                        blendState.AlphaSourceBlend = blendSrc;
                        blendState.ColorDestinationBlend = blendDst;
                        blendState.AlphaDestinationBlend = blendDst;
                        break;
                    default:
                        blendState = defaultBlendState;
                        break;
                }

                if (device.BlendState != blendState) {
                    End();
                    device.BlendState = blendState;
                }


				// calculate color
				float a = skeletonA * slot.A * attachmentColorA;
				if (premultipliedAlpha) {
					color = new Color(
							skeletonR * slot.R * attachmentColorR * a,
							skeletonG * slot.G * attachmentColorG * a,
							skeletonB * slot.B * attachmentColorB * a, a);
				}
				else {
					color = new Color(
							skeletonR * slot.R * attachmentColorR,
							skeletonG * slot.G * attachmentColorG,
							skeletonB * slot.B * attachmentColorB, a);
				}

				Color darkColor = new Color();
				if (slot.HasSecondColor) {
					if (premultipliedAlpha) {
						darkColor = new Color(slot.R2 * a, slot.G2 * a, slot.B2 * a);
					} else {
						darkColor = new Color(slot.R2 * a, slot.G2 * a, slot.B2 * a);
					}
				}
				darkColor.A = premultipliedAlpha ? (byte)255 : (byte)0;

				// clip
				if (clipper.IsClipping) {
					clipper.ClipTriangles(vertices, verticesCount << 1, indices, indicesCount, uvs);
					vertices = clipper.ClippedVertices.Items;
					verticesCount = clipper.ClippedVertices.Count >> 1;
					indices = clipper.ClippedTriangles.Items;
					indicesCount = clipper.ClippedTriangles.Count;
					uvs = clipper.ClippedUVs.Items;
				}

				if (verticesCount == 0 || indicesCount == 0)
					continue;

				// submit to batch
				MeshItem item = batcher.NextItem(verticesCount, indicesCount);
				item.texture = texture;
				for (int ii = 0, nn = indicesCount; ii < nn; ii++) {
					item.triangles[ii] = indices[ii];
				}
				VertexPositionColorTextureColor[] itemVertices = item.vertices;
				for (int ii = 0, v = 0, nn = verticesCount << 1; v < nn; ii++, v += 2) {
					itemVertices[ii].Color = color;
					itemVertices[ii].Color2 = darkColor;
					itemVertices[ii].Position.X = vertices[v];
					itemVertices[ii].Position.Y = vertices[v + 1];
					itemVertices[ii].Position.Z = 0;
					itemVertices[ii].TextureCoordinate.X = uvs[v];
					itemVertices[ii].TextureCoordinate.Y = uvs[v + 1];
					if (VertexEffect != null) VertexEffect.Transform(ref itemVertices[ii]);
				}

				clipper.ClipEnd(slot);
			}
			clipper.ClipEnd();
			if (VertexEffect != null) VertexEffect.End();
		}
	}
}
