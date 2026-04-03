export interface AcknowledgementLicenseData {
    licenseName: string;
    licenseUrl: string;
    licenseFullText: string;
}

export interface AcknowledgementData {
    title: string;
    reasons: string[];
    url: string;
    detailUrl?: string;
    integratedIntoXYVRby?: string;
    maintainer?: string;
    maintainerUrl?: string;
    kind: 'license' | 'resource';
    licenseData?: AcknowledgementLicenseData;
}

export interface Acknowledgements {
    annotation: string;
    data: AcknowledgementData[];
}
