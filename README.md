# 2.5D-Geometric-Shadows-for-Unity
2.5D lighting and shadow system that extends on current Unity 2D lighting and shadow system

Some people asked me to show the exact way of making something like this: https://forum.unity.com/threads/2-5d-lighting-using-2d-lights-isometric.1409457/#post-9283393 so I thought I'll share it alongside another method that adds "geometry" to bend shadows like this:
![image](https://github.com/venediklee/2.5D-Geometric-Shadows-for-Unity/assets/36706533/b8b809a1-0f0f-4160-b3e6-d89de2f87a03)

I use an isometric game for the project but it should work just fine with an orthographic 2d game, like [Happy Harvest](https://forum.unity.com/threads/new-urp-2d-sample-project-happy-harvest-download-now.1456480/)

Warning: this is a bit of an advanced technique(especially the bent shadows shader) and if a single part of it is done wrongly, the entire thing will not work.

# 0 - Setup and Some Notes
1. Tilemaps should be in the "background" sorting layer, which is behind the default sorting layer. Other gameobjects should be above the background sorting layer.
2. Shadow casters should only target the background sorting layer.
3. Shadow casters should not be on the same gameobject with a sprite renderer. You can put the shadow caster to the sprite renderer's parent or child.
    * This is because shadow caster erases part of light texture if that gameobject has a sprite renderer even if you don't have self shadowing enabled, which causes problems when sampling. You can probably prevent this and gain some performance with a custom render pipeline but I haven't done that.
6. Beware of the tags and layers of the gameobjects including the cameras and quads.
7. Camera priorities should be: real ground cam < large cam < main cam
8. You can write the light sample points to LightSamplePositionSetter.cs after placing the points and using context menu(right click) save etc. but I haven't bothered with it yet. It'll lower the scene size and will probably increase the performance a bit because there will be less gameobjects in the scene. You should update the light sample points if your large object is dynamic which isn't yet implemented in LightSamplePositionSetter.cs(it is implemented for editor, not build)
9. You should probably use 2023.1 beta or compatible Unity version, it has some features like shadow caster's casting source etc. that are in use by this package.
10. Set light render scale to 1 in renderer2d data.
11. Flipping the sprites will probably not work properly, you may have trouble if you flip the gameobject if you are using isometric shadowed large shader but I haven't tested it.
12. (just in case you haven't done this yet)Your project's transparency sort axis should be (0,1,0)
13. In real games you should use chunk or SRP batch mode for tilemaps for performance.
14. You can rotate the light2d's by 30-60 degrees on the X axis if you want them to be a bit more realistic. This is because the game is viewed at an angle, so rotating the lights makes them have the same actual X and Y scale.
15. Use proper grid snapping, pixel pivot on sprites, sort mode: pivot on sprite renderers, pixel perfect camera settings etc. if you want pixel perfect camera. [Read the details here](https://docs.unity3d.com/Packages/com.unity.2d.pixel-perfect@4.0/manual/index.html)
16. If you use cinemachine camera dont forget to add the virtual camera's layer to main camera's culling layer.
17. I use the pixel perfect camera to automatically calculate a base size for the main camera. Then, you calculate large cam's size by multiplying orthographic size of the main camera with the amount of padding you want. In the example main camera's size is 1.40625, and I used 1.2 multiplier, which means large camera's size is 1.6875. The render texture for the output of large camera's size is (1920x1080)*1.2 . The Y scale of the quad gameobject under large cam is the size of large cam*2, the X scale of the quad is calculated by multiplying it with aspect ratio, so it is (size of large cam * 2 * 16 / 9). I also moved the large cam by -0.2*main camera's ortho size on the Y axis to move the padding to below the main camera. I do this since we only sample the light texture below the main camera not above it. If you have problems with sprites that are near the top of the screen, you should move the large cam a bit above or increase its size by a bigger multiplier.
    * camera ***size*** is under the camera's projection settings. Don't confuse it with gameobject ***scale***.
    * Yes, you need to adjust the render texture's sizes/scales manually if the player changes screen resolution etc.
18. These shadowing effects wont look 100% correct with [shadows that have "length"}(https://forum.unity.com/threads/2d-lighting-shadows.826584/)
19. Odds are you can use rendering layer masks instead of gameobject layers for all this effects but I haven't tried it, I am not even 50% sure if that'll work.


# 1 - light texture(s) explained
Light texture(s) is the main source of this rendering technique. It looks like this for the example scene above:
![image](https://github.com/venediklee/2.5D-Geometric-Shadows-for-Unity/assets/36706533/29aa7322-9cb5-45b3-b80d-f5bf82422f2d)

These are created automatically by Unity. It is not guaranteed that your _LightTexture_0_X will be like the one above, as the first number changes depending on multiple factors like if you have shadow casters or not. Search for "light2d" in the frame debugger and check the "render target"s in each result. If your light texture is not _LightTexture_0_X you'll have to change it to _LightTexture_1_X etc.

Note: if you want light types other than spot light to effect the outcome of the shader, you'll need to change the light texture to its corresponding one, like _LightTexture_1_X
Note2: there will be _LightTexture_0_0 to _LightTexture_0_3 each corresponding to one of your light blend styles in the renderer 2d data. If you want other light blending styles to effect the outcome of the shader, you'll need to change the light texture to its corresponding one.
Note3: you can blend between multiple light textures in your custom shader. Note that sampling textures take GPU time.

# 2 - Pillars - Simple Shadowed
**This part explains how the pillars are rendered/shadowed.**

![image](https://github.com/venediklee/2.5D-Geometric-Shadows-for-Unity/assets/36706533/386e7944-bcd1-4e71-86a8-120c378f3497)

The pillars are rendered by sampling the light texture just a bit below its pivot point and multiplying the main color with the light texture's color. This adds a bit of darkness when a pillar is in the shadow of an object(including its own shadow that happens when a light is above it).

The problem with sampling light texture in a fixed position is the fact that light texture only contains light information of the camera bounds/frustum, so sampling below the camera bounds will not be correct.

The solution is rendering a larger than screen size with another camera(LargeCam) to a render texture(LargeRender), and rendering ***only*** LargeRender with main camera.

Note: LargeCam is in default layer, the quad that renders LargeRender is in LargeView layer.
Note2: LargeCam's culling layer is everything except LargeView.
Note3: Main camera's culling layer is LargeView.
Note4: Size calculations of LargeCam and LargeRender and scale calculations of the quad that has LargeRender is explained in section 0
Note4: The shadowing on the pillar depends on the softness of the shadow! So dynamic objects that are passing to and from a shadow will gradually darken/lighten.

# 3 - House - Large Shadowed
**This part explains how large objects that needs more than one light sampling point is rendered/shadowed.**

![image](https://github.com/venediklee/2.5D-Geometric-Shadows-for-Unity/assets/36706533/58ca1bdb-d25b-4e86-afc2-d22faa702bb0)

Large objects are rendered by sampling the light texture along a manually adjusted line and multiplying the main color with the light texture's color. This adds a bit of darkness when a large object is in the shadow of an object(including its own shadow that happens when a light is above it). As a side effect, this adds "bent" shadows.

The problem with sampling along a line is it is very expensive and not definitive(when edges are parallel) to calculate the interpolation value. Normally you would need to define edges of the object, then use quadrilateral inverse bilinear interpolation to find the interpolation value[0,1] and use it to interpolate between the given points. Instead, we "bake" it to an "Edge Map" and sample the edge map to directly get the interpolation value. Baking is quite easy to do, all you have to do is adjust the edge map by using perspective transformation etc. using the following base image on most image editing software, including [Krita, which is free.](https://krita.org/en/about/license/)

![image](https://github.com/venediklee/2.5D-Geometric-Shadows-for-Unity/assets/36706533/6d0689c9-6bae-481f-823c-e3531a39089e)
> the darker side of the edge map means sample closer to the "LightSampleLeftPoint", brighter side of the edge map means sample closer to "LightSampleRightPoint"

There will be multiple layers of this image each covering one angle. For example, the house has an edge map like this:
![image](https://github.com/venediklee/2.5D-Geometric-Shadows-for-Unity/assets/36706533/7107ccb8-3582-4ccb-bdbb-cd76061d0ed8)

As you can see the front, side, roof edge, roof top all have separate layers for the edge map for even more detail. You can also reduce layer opacity during editing to fine tune it a bit easily. You can also enable "inherit alpha"(the alpha button next to each layer) etc. to fit the edge map to the main layer but you don't need to.
Don't forget to increase alpha back to 100 before exporting the edge map.

Odds are the edge map will be a bit off first time you do it(the shadows dont bend at the exact spot etc.), so if you dont do it properly in the first time, just go back and edit it.

# 4 - Ground With Non Flat Normal Maps"]
If you have a ground tile that has non flat normal map, the light texture will not be smooth, causing massive shadow problems like this:
![image](https://github.com/venediklee/2.5D-Geometric-Shadows-for-Unity/assets/36706533/7d03cba1-ba67-4f1e-bb62-ccc949e94333)

The solution is creating a tilemap with flat tiles(FlatTilemap at FlatGround layer) that is exactly the same as the real ground, rendering the FlatTilemap along with every other gameobject and using another camera(RealGroundCam) to render the actual ground(RealTilemap at RealGround layer), above the FlatTilemap.

When you add a sorting group component to the RealGroundCam and set the sorting layer to Background with the highest sorting order it will render the real ground above the flat ground tiles and behind other gameobjects.

So, RealGroundCam will have RealGround and Light2D in its culling mask,
LargeCam will have every layer except RealGround and LargeView culling mask,
MainCam will have LargeView in its culling mask
Quad of RealGroundCam will have Z position at 2, Quad of LargeCam will have Z position at 1

Note: you don't need to do this if your ground doesn't have non-flat normal maps.
Note2: RealGroundCam doesn't have to be large.

Let me know if I missed something!

# Closing Thoughts
You get all this for free if you render 3d xdd

The house is rendered from [this](https://www.turbosquid.com/3d-models/cartoon-wood-house-3d-model-1660074)
