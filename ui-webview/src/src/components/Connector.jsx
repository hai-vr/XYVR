import React from 'react';
import Account from './Account.jsx';
import './Connector.css';

const Connector = ({ connector, onDeleteClick, deleteState }) => {
    return (
        <div className="connector-card">
            {connector.account && (
                <Account account={connector.account} />
            )}

            <div className="connector-actions">
                <button
                    title="Update"
                >
                    📋 Update TODO
                </button>
                <button
                    className={`delete-button ${deleteState?.confirming ? '' : ''}`}
                    onClick={() => onDeleteClick(connector.guid)}
                    title={deleteState?.confirming ? 'Click again to confirm delete' : 'Delete connector (requires double-click)'}
                >
                    {deleteState?.confirming ? '⚠️ Really remove?' : '🗑️ Remove'}
                </button>
            </div>
        </div>
    );
};

export default Connector;
