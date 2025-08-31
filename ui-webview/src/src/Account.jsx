import React from 'react';

const Account = ({ account }) => {
    const hasNote = account.note && account.note.status === 1 && account.note.text;

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
                        background: account.namedApp === 1 ? '#ff6b35' :
                            account.namedApp === 2 ? '#1778f2' : '#6c757d',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        marginRight: '12px',
                        fontSize: '14px'
                    }}>
                        {account.namedApp === 1 ? '⚡' :
                            account.namedApp === 2 ? '💬' : '❓'}
                    </div>
                    <div>
                        <div style={{
                            fontWeight: '600',
                            color: '#2c3e50',
                            fontSize: '14px'
                        }}>
                            {account.inAppDisplayName}
                        </div>
                        {account.qualifiedAppName && (
                            <div style={{
                                color: '#6c757d',
                                fontSize: '12px',
                                marginTop: '2px'
                            }}>
                                {account.namedApp === 1 ? 'Resonite' :
                                    account.namedApp === 2 ? 'VRChat' : account.qualifiedAppName}
                            </div>
                        )}
                    </div>
                </div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    {!account.isContact && hasNote && (
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
                    {account.isContact && (
                        <span style={{
                            background: '#fff3cd',
                            color: '#856404',
                            padding: '2px 6px',
                            borderRadius: '4px',
                            fontSize: '11px',
                            fontWeight: '500'
                        }}>
                            Contact
                        </span>
                    )}
                </div>
            </div>
            
            {hasNote && (
                <div style={{
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
                        {account.note.text.startsWith('mt ') ? ('Met through ' + account.note.text.substring(3)) : account.note.text}
                    </div>
                </div>
            )}
        </div>
    );
};

export default Account;