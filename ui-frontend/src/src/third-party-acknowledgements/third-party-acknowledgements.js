export default {
    annotation: `
This is NOT a LICENSE file.

This credits.js file is used to generate the ThirdPartyAcknowledgements page in the XYVR UI, but they do not reflect the license of the XYVR project.
For more info, open the README.md at https://github.com/hai-vr/XYVR/README.md
`,
    data: [
        {
            "title": "VRCX",
            "reasons": ["`parseLocation` function of VRCX"],
            "url" : "https://github.com/vrcx-team/VRCX/",
            "detailUrl" : "https://github.com/vrcx-team/VRCX/blob/master/src/shared/utils/location.js#L35C1-L145C2",
            "integratedIntoXYVRby": "github.com/hai-vr",
            "maintainer": "VRCX Team",
            "maintainerUrl": "https://github.com/vrcx-team",
            "kind": "license",
            "licenseData": {
                "licenseName" : "MIT License",
                "licenseUrl" : "https://github.com/vrcx-team/VRCX/blob/dda3d2dda9c8f4c840f230072f2ebefb72d58623/LICENSE",
                "licenseFullText": `
MIT License

Copyright (c) 2019-2025 pypy and individual contributors.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
`
            }
        },
        {
            "title": "CVRX",
            "reasons": ["Due to the absence of documentation, the CVRX app by AstroDogeDX was used as a reference for the API endpoints."],
            "url": "https://github.com/AstroDogeDX/CVRX/",
            "detailUrl": "https://github.com/AstroDogeDX/CVRX/blob/472cceec651abbeff9c76ae8412522d27015bfd9/server/api_cvr_http.js",
            "integratedIntoXYVRby": "github.com/art0007i",
            "maintainer": "github.com/AstroDogeDX",
            "maintainerUrl": "https://github.com/AstroDogeDX",
            "kind": "license",
            "licenseData": {
                "licenseName" : "MIT License",
                "licenseUrl":"https://github.com/AstroDogeDX/CVRX/blob/472cceec651abbeff9c76ae8412522d27015bfd9/LICENSE",
                "licenseFullText": `
MIT License

Copyright (c) 2023 AstroDogeDX

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
`
            }
        },
        {
            "title": "Resonite wiki",
            "reasons": ["API documentation"],
            "url": "https://wiki.resonite.com/API",
            "integratedIntoXYVRby": "github.com/hai-vr",
            "maintainer": "Resonite",
            "maintainerUrl": "https://wiki.resonite.com/",
            "kind": "resource"
        },
        {
            "title": "Community-maintained VRChat API documentation",
            "reasons": ["API documentation"],
            "url": "https://vrchat.community",
            "integratedIntoXYVRby": "github.com/hai-vr",
            "maintainer": "github.com/vrchatapi",
            "maintainerUrl": "https://github.com/vrchatapi",
            "kind": "resource"
        }
    ]
}