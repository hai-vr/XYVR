import React from 'react';

const Account = ({ account }) => {
    const hasNote = account.isAnyCallerNote;

    const copyInAppIdentifier = async () => {
        await navigator.clipboard.writeText(account.inAppIdentifier);
    };

    return (
        <div style={{
            background: '#f8f9fa',
            border: '1px solid #e9ecef',
            borderRadius: '8px',
            padding: '12px',
            display: 'flex',
            flexDirection: 'column',
            gap: '8px'
        }}>
            <div style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between'
            }}>
                <div style={{ display: 'flex', alignItems: 'center' }}>
                    <div style={{
                        width: '32px',
                        height: '32px',
                        borderRadius: '6px',
                        background: account.namedApp === "Resonite" ? 'linear-gradient(170deg, #84ea22 20%, #fff834 80%)' :
                            account.namedApp === "VRChat" ? '#186dcd' :
                            account.namedApp === "Cluster" ? '#02a8ea' :
                                '#6c757d',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        marginRight: '12px',
                        fontSize: '14px'
                    }}>
                        {account.namedApp === "Resonite" ? '⚡' :
                            account.namedApp === "VRChat" ? '💬' :
                            account.namedApp === "Cluster" ? '☁️' : '❓'}
                    </div>
                    <div>
                        <div style={{
                            fontWeight: '600',
                            color: '#2c3e50',
                            fontSize: '14px',
                            textAlign: 'left'
                        }}>
                            {account.inAppDisplayName}
                        </div>
                        <div style={{
                            color: '#6c757d',
                            fontSize: '12px',
                            marginTop: '2px',
                            textAlign: 'left'
                        }}>
                            {account.namedApp === "Resonite" ? 'Resonite' :
                                account.namedApp === "VRChat" ? 'VRChat' :
                                account.namedApp === "Cluster" ? 'Cluster (@' + account.inAppIdentifier + ')' : account.qualifiedAppName}
                        </div>
                    </div>
                </div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    {!account.isAnyCallerContact && hasNote && (
                        <span style={{
                            background: '#e3f2fd',
                            color: '#1976d2',
                            padding: '2px 6px',
                            borderRadius: '4px',
                            fontSize: '11px',
                            fontWeight: '500'
                        }}>
                            📝 Note
                        </span>
                    )}
                    {account.isAnyCallerContact && (
                        <span style={{
                            background: '#fff3cd',
                            color: '#856404',
                            padding: '2px 6px',
                            borderRadius: '4px',
                            fontSize: '11px',
                            fontWeight: '500'
                        }}>
                            {account.namedApp === "VRChat" ? 'Friend' : 'Contact'}
                        </span>
                    )}
                    {account.isTechnical && (
                        <span style={{
                            background: '#f8d7da',
                            color: '#721c24',
                            padding: '2px 6px',
                            borderRadius: '4px',
                            fontSize: '11px',
                            fontWeight: '500'
                        }}>
                            Bot
                        </span>
                    )}
                    <button
                        onClick={copyInAppIdentifier}
                        style={{
                            background: '#f8f9fa',
                            border: '1px solid #dee2e6',
                            borderRadius: '4px',
                            padding: '4px 6px',
                            fontSize: '11px',
                            color: '#6c757d',
                            cursor: 'pointer',
                            display: 'flex',
                            alignItems: 'center',
                            transition: 'all 0.2s ease'
                        }}
                        onMouseEnter={(e) => {
                            e.target.style.background = '#e9ecef';
                            e.target.style.borderColor = '#adb5bd';
                        }}
                        onMouseLeave={(e) => {
                            e.target.style.background = '#f8f9fa';
                            e.target.style.borderColor = '#dee2e6';
                        }}
                        title={`Copy ID: ${account.inAppIdentifier}`}
                    >
                        📋
                    </button>
                </div>
            </div>

            {account.callers && account.callers.filter(caller => caller.note.status === "Exists").map((caller, index) => (
                <div key={index} style={{
                    background: '#f0f8ff',
                    border: '1px solid #b3d9ff',
                    borderRadius: '6px',
                    padding: '8px',
                    marginTop: '4px'
                }}>
                    <div style={{
                        fontSize: '12px',
                        color: '#1976d2',
                        fontWeight: '500',
                        marginBottom: '4px'
                    }}>
                        📝 Note:
                    </div>
                    <div style={{
                        fontSize: '13px',
                        color: '#2c3e50',
                        lineHeight: '1.4',
                        whiteSpace: 'pre-wrap'
                    }}>
                        {caller.note.text.startsWith('mt ') ? ('Met through ' + caller.note.text.substring(3)) : caller.note.text}
                    </div>
                </div>
            ))}
        </div>
    );
};

export default Account;